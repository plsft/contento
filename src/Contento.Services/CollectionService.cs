using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for managing pSEO collections — groups of pages generated from niche x subtopic combinations.
/// </summary>
public class CollectionService : ICollectionService
{
    private readonly IDbConnection _db;
    private readonly INicheService _nicheService;
    private readonly IContentSchemaService _schemaService;
    private readonly ILogger<CollectionService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="CollectionService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="nicheService">The niche service.</param>
    /// <param name="schemaService">The content schema service.</param>
    /// <param name="logger">The logger.</param>
    public CollectionService(IDbConnection db, INicheService nicheService, IContentSchemaService schemaService, ILogger<CollectionService> logger)
    {
        _db = Guard.Against.Null(db);
        _nicheService = Guard.Against.Null(nicheService);
        _schemaService = Guard.Against.Null(schemaService);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task<PseoCollection?> GetByIdAsync(Guid id)
    {
        Guard.Against.Default(id);
        return await _db.GetAsync<PseoCollection>(id);
    }

    /// <inheritdoc />
    public async Task<List<PseoCollection>> GetByProjectIdAsync(Guid projectId)
    {
        Guard.Against.Default(projectId);

        var results = await _db.QueryAsync<PseoCollection>(
            "SELECT * FROM pseo_collections WHERE project_id = @ProjectId ORDER BY created_at DESC",
            new { ProjectId = projectId });
        return results.ToList();
    }

    /// <inheritdoc />
    public async Task<PseoCollection> CreateAsync(PseoCollection collection)
    {
        Guard.Against.Null(collection);
        Guard.Against.NullOrWhiteSpace(collection.Name);
        Guard.Against.Default(collection.ProjectId);
        Guard.Against.Default(collection.SchemaId);

        collection.Id = Guid.NewGuid();
        collection.CreatedAt = DateTime.UtcNow;
        collection.UpdatedAt = DateTime.UtcNow;

        await _db.InsertAsync(collection);
        return collection;
    }

    /// <inheritdoc />
    public async Task<PseoCollection> UpdateAsync(PseoCollection collection)
    {
        Guard.Against.Null(collection);
        Guard.Against.Default(collection.Id);

        collection.UpdatedAt = DateTime.UtcNow;
        await _db.UpdateAsync(collection);
        return collection;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        Guard.Against.Default(id);

        var collection = await _db.GetAsync<PseoCollection>(id);
        if (collection != null)
            await _db.DeleteAsync(collection);
    }

    /// <inheritdoc />
    public async Task UpdateStatusAsync(Guid id, string status)
    {
        Guard.Against.Default(id);
        Guard.Against.NullOrWhiteSpace(status);

        await _db.ExecuteAsync(
            "UPDATE pseo_collections SET status = @Status, updated_at = @Now WHERE id = @Id",
            new { Status = status, Now = DateTime.UtcNow, Id = id });
    }

    /// <inheritdoc />
    public async Task UpdateCountsAsync(Guid id, int generated, int published, int failed)
    {
        Guard.Against.Default(id);

        await _db.ExecuteAsync(
            @"UPDATE pseo_collections
              SET generated_count = @Generated, published_count = @Published,
                  failed_count = @Failed, updated_at = @Now
              WHERE id = @Id",
            new { Generated = generated, Published = published, Failed = failed, Now = DateTime.UtcNow, Id = id });
    }

    /// <inheritdoc />
    public async Task<List<PseoCollectionNiche>> GetNichesAsync(Guid collectionId)
    {
        Guard.Against.Default(collectionId);

        var results = await _db.QueryAsync<PseoCollectionNiche>(
            "SELECT * FROM pseo_collection_niches WHERE collection_id = @CollectionId",
            new { CollectionId = collectionId });
        return results.ToList();
    }

    /// <inheritdoc />
    public async Task SetNichesAsync(Guid collectionId, List<PseoCollectionNiche> niches)
    {
        Guard.Against.Default(collectionId);
        Guard.Against.Null(niches);

        // Delete existing niche associations
        await _db.ExecuteAsync(
            "DELETE FROM pseo_collection_niches WHERE collection_id = @CollectionId",
            new { CollectionId = collectionId });

        // Insert new associations
        foreach (var niche in niches)
        {
            niche.Id = Guid.NewGuid();
            niche.CollectionId = collectionId;
            niche.CreatedAt = DateTime.UtcNow;
            await _db.InsertAsync(niche);
        }
    }

    /// <inheritdoc />
    public async Task<List<PseoPage>> ExpandSubtopicsAsync(Guid collectionId)
    {
        Guard.Against.Default(collectionId);

        var collection = await _db.GetAsync<PseoCollection>(collectionId)
            ?? throw new InvalidOperationException($"Collection {collectionId} not found");

        var collectionNiches = await GetNichesAsync(collectionId);
        var pages = new List<PseoPage>();

        foreach (var cn in collectionNiches)
        {
            var niche = await _nicheService.GetByIdAsync(cn.NicheId);
            if (niche == null) continue;

            var subtopics = JsonSerializer.Deserialize<List<string>>(cn.Subtopics) ?? new List<string>();

            foreach (var subtopic in subtopics)
            {
                var slug = BuildSlug(collection.UrlPattern, niche.Slug, subtopic);
                var title = BuildTitle(collection.TitleTemplate, niche.Name, subtopic);

                var page = new PseoPage
                {
                    Id = Guid.NewGuid(),
                    CollectionId = collectionId,
                    ProjectId = collection.ProjectId,
                    NicheSlug = niche.Slug,
                    Subtopic = subtopic,
                    Slug = slug,
                    Title = title,
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                pages.Add(page);
            }
        }

        _logger.LogInformation("Expanded collection {CollectionId} into {PageCount} page stubs",
            collectionId, pages.Count);

        return pages;
    }

    /// <summary>
    /// Builds a URL slug from the collection's URL pattern, replacing {niche} and {subtopic} placeholders.
    /// </summary>
    private static string BuildSlug(string urlPattern, string nicheSlug, string subtopic)
    {
        var slug = urlPattern
            .Replace("{niche}", nicheSlug, StringComparison.OrdinalIgnoreCase)
            .Replace("{subtopic}", Slugify(subtopic), StringComparison.OrdinalIgnoreCase);
        return slug.Trim('/');
    }

    /// <summary>
    /// Builds a page title from the collection's title template, replacing {niche} and {subtopic} placeholders.
    /// </summary>
    private static string BuildTitle(string titleTemplate, string nicheName, string subtopic)
    {
        return titleTemplate
            .Replace("{niche}", nicheName, StringComparison.OrdinalIgnoreCase)
            .Replace("{subtopic}", subtopic, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Converts a string into a URL-friendly slug.
    /// </summary>
    private static string Slugify(string text)
    {
        var slug = text.ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"[\s-]+", "-");
        slug = slug.Trim('-');
        return string.IsNullOrEmpty(slug) ? "page" : slug;
    }
}
