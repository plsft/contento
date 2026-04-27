using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for orchestrating AI content generation across pSEO collections and individual pages.
/// </summary>
public interface IGenerationService
{
    /// <summary>
    /// Triggers a full generation run for all pages in a collection.
    /// </summary>
    /// <param name="collectionId">The collection to generate content for.</param>
    /// <param name="ct">Cancellation token to support graceful shutdown.</param>
    Task GenerateCollectionAsync(Guid collectionId, CancellationToken ct);

    /// <summary>
    /// Regenerates content for a single page.
    /// </summary>
    /// <param name="pageId">The page to regenerate.</param>
    /// <param name="ct">Cancellation token to support graceful shutdown.</param>
    Task GenerateSinglePageAsync(Guid pageId, CancellationToken ct);

    /// <summary>
    /// Returns the current generation progress for a collection.
    /// </summary>
    /// <param name="collectionId">The collection to check progress for.</param>
    /// <returns>Progress information including counts and status.</returns>
    Task<GenerationProgress> GetProgressAsync(Guid collectionId);
}

/// <summary>
/// Snapshot of generation progress for a collection.
/// </summary>
public class GenerationProgress
{
    public Guid CollectionId { get; set; }
    public int TotalPages { get; set; }
    public int Generated { get; set; }
    public int Failed { get; set; }
    public int Pending { get; set; }
    public string Status { get; set; } = "";
}
