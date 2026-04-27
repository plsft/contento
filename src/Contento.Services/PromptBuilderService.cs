using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for constructing AI prompts from schemas, niches, and subtopics for pSEO content generation.
/// </summary>
public class PromptBuilderService : IPromptBuilderService
{
    private readonly ILogger<PromptBuilderService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PromptBuilderService"/>.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public PromptBuilderService(ILogger<PromptBuilderService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<string> BuildSystemPromptAsync(ContentSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var prompt = $"""
            You are a structured content generator. You MUST respond with valid JSON only.
            No markdown, no preamble, no explanation. The response must match this exact schema:
            {schema.SchemaJson}

            {schema.PromptTemplate}

            Total response must be valid JSON parseable by JSON.parse()
            """;

        _logger.LogDebug("Built system prompt for schema {SchemaSlug} ({Length} chars)", schema.Slug, prompt.Length);

        return Task.FromResult(prompt);
    }

    /// <inheritdoc />
    public Task<string> BuildUserPromptAsync(string title, NicheTaxonomy niche, string subtopic)
    {
        ArgumentNullException.ThrowIfNull(niche);

        var prompt = $"""
            Generate content for: {title}
            Niche context: {niche.Context}
            Subtopic focus: {subtopic}
            """;

        _logger.LogDebug("Built user prompt for title '{Title}', niche '{Niche}', subtopic '{Subtopic}'",
            title, niche.Slug, subtopic);

        return Task.FromResult(prompt);
    }
}
