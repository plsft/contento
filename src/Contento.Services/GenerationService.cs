using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Orchestrates AI content generation across pSEO collections and individual pages.
/// </summary>
public class GenerationService : IGenerationService
{
    private readonly IAiService _aiService;
    private readonly ICollectionService _collectionService;
    private readonly IPseoPageService _pageService;
    private readonly IContentSchemaService _schemaService;
    private readonly ISchemaValidationService _validationService;
    private readonly IPromptBuilderService _promptBuilder;
    private readonly INicheService _nicheService;
    private readonly ILogger<GenerationService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="GenerationService"/>.
    /// </summary>
    public GenerationService(
        IAiService aiService,
        ICollectionService collectionService,
        IPseoPageService pageService,
        IContentSchemaService schemaService,
        ISchemaValidationService validationService,
        IPromptBuilderService promptBuilder,
        INicheService nicheService,
        ILogger<GenerationService> logger)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _collectionService = collectionService ?? throw new ArgumentNullException(nameof(collectionService));
        _pageService = pageService ?? throw new ArgumentNullException(nameof(pageService));
        _schemaService = schemaService ?? throw new ArgumentNullException(nameof(schemaService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
        _nicheService = nicheService ?? throw new ArgumentNullException(nameof(nicheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task GenerateCollectionAsync(Guid collectionId, CancellationToken ct)
    {
        var collection = await _collectionService.GetByIdAsync(collectionId)
            ?? throw new InvalidOperationException($"Collection {collectionId} not found");

        var schema = await _schemaService.GetByIdAsync(collection.SchemaId)
            ?? throw new InvalidOperationException($"Content schema {collection.SchemaId} not found");

        _logger.LogInformation("Starting generation for collection {CollectionId} ({Name})", collectionId, collection.Name);

        await _collectionService.UpdateStatusAsync(collectionId, "generating");

        // Expand niches x subtopics into page stubs
        var pageStubs = await _collectionService.ExpandSubtopicsAsync(collectionId);
        if (pageStubs.Count == 0)
        {
            _logger.LogWarning("No page stubs expanded for collection {CollectionId}", collectionId);
            await _collectionService.UpdateStatusAsync(collectionId, "draft");
            return;
        }

        // Bulk-create the stubs
        var pages = await _pageService.BulkCreateAsync(pageStubs);
        _logger.LogInformation("Created {Count} page stubs for collection {CollectionId}", pages.Count, collectionId);

        // Track progress
        var generated = 0;
        var failed = 0;

        // Generate content for each page with bounded parallelism
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 10,
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(pages, options, async (page, token) =>
        {
            try
            {
                await GenerateSinglePageAsync(page.Id, token);
                Interlocked.Increment(ref generated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate page {PageId}", page.Id);
                Interlocked.Increment(ref failed);
            }

            // Update collection counts periodically
            var currentGenerated = Volatile.Read(ref generated);
            var currentFailed = Volatile.Read(ref failed);
            await _collectionService.UpdateCountsAsync(collectionId, currentGenerated, collection.PublishedCount, currentFailed);
        });

        // Final count update
        await _collectionService.UpdateCountsAsync(collectionId, generated, collection.PublishedCount, failed);

        var finalStatus = failed == pages.Count ? "failed" : "generated";
        await _collectionService.UpdateStatusAsync(collectionId, finalStatus);

        _logger.LogInformation(
            "Collection {CollectionId} generation complete: {Generated} generated, {Failed} failed out of {Total}",
            collectionId, generated, failed, pages.Count);
    }

    /// <inheritdoc />
    public async Task GenerateSinglePageAsync(Guid pageId, CancellationToken ct)
    {
        var page = await _pageService.GetByIdAsync(pageId)
            ?? throw new InvalidOperationException($"Page {pageId} not found");

        var collection = await _collectionService.GetByIdAsync(page.CollectionId)
            ?? throw new InvalidOperationException($"Collection {page.CollectionId} not found");

        var schema = await _schemaService.GetByIdAsync(collection.SchemaId)
            ?? throw new InvalidOperationException($"Content schema {collection.SchemaId} not found");

        var niche = await _nicheService.GetBySlugAsync(page.NicheSlug);

        // Build prompts
        var systemPrompt = await _promptBuilder.BuildSystemPromptAsync(schema);
        var userPrompt = niche != null
            ? await _promptBuilder.BuildUserPromptAsync(page.Title, niche, page.Subtopic)
            : $"Generate content for: {page.Title}\nSubtopic focus: {page.Subtopic}";

        // Resolve AI settings from collection settings
        var aiSettings = ResolveAiSettings(collection.Settings);

        var sw = Stopwatch.StartNew();

        // Attempt generation (with one retry on validation failure)
        var maxAttempts = 2;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            var result = await _aiService.CompleteAsync(systemPrompt, userPrompt, aiSettings);

            if (!result.Success)
            {
                _logger.LogWarning("AI completion failed for page {PageId}: {Error}", pageId, result.Error);

                if (attempt < maxAttempts - 1)
                {
                    page.RetryCount++;
                    await _pageService.UpdateAsync(page);
                    continue;
                }

                // Final failure
                sw.Stop();
                page.Status = "failed";
                page.GenerationDurationMs = (int)sw.ElapsedMilliseconds;
                page.ValidationErrors = JsonSerializer.Serialize(new[] { result.Error ?? "AI completion failed" });
                page.UpdatedAt = DateTime.UtcNow;
                await _pageService.UpdateAsync(page);
                return;
            }

            // Parse the JSON response
            var contentJson = ExtractJson(result.Text);

            // Validate against schema
            var (isValid, errors) = await _validationService.ValidateAsync(schema.SchemaJson, contentJson);

            if (!isValid && attempt < maxAttempts - 1)
            {
                _logger.LogWarning("Validation failed for page {PageId} (attempt {Attempt}): {Errors}",
                    pageId, attempt + 1, string.Join("; ", errors));
                page.RetryCount++;
                await _pageService.UpdateAsync(page);
                continue;
            }

            // Also run field constraint validation
            if (isValid)
            {
                var (constraintsValid, constraintErrors) = await _validationService.ValidateFieldConstraintsAsync(schema.SchemaJson, contentJson);
                if (!constraintsValid)
                {
                    if (attempt < maxAttempts - 1)
                    {
                        _logger.LogWarning("Constraint validation failed for page {PageId} (attempt {Attempt}): {Errors}",
                            pageId, attempt + 1, string.Join("; ", constraintErrors));
                        page.RetryCount++;
                        await _pageService.UpdateAsync(page);
                        continue;
                    }

                    errors.AddRange(constraintErrors);
                    isValid = false;
                }
            }

            sw.Stop();
            page.GenerationDurationMs = (int)sw.ElapsedMilliseconds;

            if (isValid)
            {
                page.ContentJson = contentJson;
                page.Status = "validated";
                page.ValidationErrors = "[]";
                page.WordCount = CountWords(contentJson);
                page.UpdatedAt = DateTime.UtcNow;
                await _pageService.UpdateAsync(page);

                _logger.LogDebug("Page {PageId} generated and validated in {Duration}ms", pageId, page.GenerationDurationMs);
            }
            else
            {
                page.Status = "failed";
                page.ValidationErrors = JsonSerializer.Serialize(errors);
                page.UpdatedAt = DateTime.UtcNow;
                await _pageService.UpdateAsync(page);

                _logger.LogWarning("Page {PageId} failed validation after all retries: {Errors}",
                    pageId, string.Join("; ", errors));
            }

            return;
        }
    }

    /// <inheritdoc />
    public async Task<GenerationProgress> GetProgressAsync(Guid collectionId)
    {
        var collection = await _collectionService.GetByIdAsync(collectionId);
        if (collection == null)
        {
            return new GenerationProgress
            {
                CollectionId = collectionId,
                Status = "not_found"
            };
        }

        // Get actual page counts by status
        var allPages = await _pageService.GetByCollectionIdAsync(collectionId, null, 1, int.MaxValue);
        var generated = allPages.Count(p => p.Status == "validated" || p.Status == "published");
        var failed = allPages.Count(p => p.Status == "failed");
        var pending = allPages.Count(p => p.Status == "pending" || p.Status == "generating");

        return new GenerationProgress
        {
            CollectionId = collectionId,
            TotalPages = allPages.Count,
            Generated = generated,
            Failed = failed,
            Pending = pending,
            Status = collection.Status
        };
    }

    /// <summary>
    /// Resolves AI settings from collection settings JSON.
    /// </summary>
    private static AiSettings ResolveAiSettings(string settingsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(settingsJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("ai", out var aiElement))
            {
                return new AiSettings
                {
                    Provider = aiElement.TryGetProperty("provider", out var p) ? p.GetString() ?? "openai" : "openai",
                    Model = aiElement.TryGetProperty("model", out var m) ? m.GetString() ?? "openai/gpt-4o-mini" : "openai/gpt-4o-mini",
                    ApiKey = aiElement.TryGetProperty("apiKey", out var k) ? k.GetString() ?? "" : "",
                    Enabled = aiElement.TryGetProperty("enabled", out var e) && e.GetBoolean()
                };
            }
        }
        catch
        {
            // Fall through to defaults
        }

        return new AiSettings();
    }

    /// <summary>
    /// Extracts a JSON object from AI response text that may contain markdown fences or preamble.
    /// </summary>
    private static string ExtractJson(string text)
    {
        // Try to find JSON in markdown code fence
        var fenceMatch = Regex.Match(text, @"```(?:json)?\s*\n?([\s\S]*?)\n?```", RegexOptions.Singleline);
        if (fenceMatch.Success)
            return fenceMatch.Groups[1].Value.Trim();

        // Try to find a top-level JSON object
        var braceStart = text.IndexOf('{');
        var braceEnd = text.LastIndexOf('}');
        if (braceStart >= 0 && braceEnd > braceStart)
            return text[braceStart..(braceEnd + 1)];

        return text.Trim();
    }

    /// <summary>
    /// Estimates word count from JSON content by extracting all string values.
    /// </summary>
    private static int CountWords(string contentJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(contentJson);
            var wordCount = 0;
            CountWordsInElement(doc.RootElement, ref wordCount);
            return wordCount;
        }
        catch
        {
            return 0;
        }
    }

    private static void CountWordsInElement(JsonElement element, ref int wordCount)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var text = element.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    wordCount += text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                }
                break;

            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    CountWordsInElement(prop.Value, ref wordCount);
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CountWordsInElement(item, ref wordCount);
                }
                break;
        }
    }
}
