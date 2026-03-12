using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing pSEO collections — groups of pages generated from niche × subtopic combinations.
/// </summary>
public interface ICollectionService
{
    /// <summary>
    /// Retrieves a collection by its unique identifier.
    /// </summary>
    Task<PseoCollection?> GetByIdAsync(Guid id);

    /// <summary>
    /// Returns all collections belonging to a specific project.
    /// </summary>
    Task<List<PseoCollection>> GetByProjectIdAsync(Guid projectId);

    /// <summary>
    /// Creates a new collection.
    /// </summary>
    Task<PseoCollection> CreateAsync(PseoCollection collection);

    /// <summary>
    /// Updates an existing collection.
    /// </summary>
    Task<PseoCollection> UpdateAsync(PseoCollection collection);

    /// <summary>
    /// Deletes a collection and its associated data.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Updates the status of a collection (e.g., draft, generating, published).
    /// </summary>
    Task UpdateStatusAsync(Guid id, string status);

    /// <summary>
    /// Updates the generation/publish progress counters for a collection.
    /// </summary>
    Task UpdateCountsAsync(Guid id, int generated, int published, int failed);

    /// <summary>
    /// Retrieves all niche assignments for a collection.
    /// </summary>
    Task<List<PseoCollectionNiche>> GetNichesAsync(Guid collectionId);

    /// <summary>
    /// Replaces all niche assignments for a collection.
    /// </summary>
    Task SetNichesAsync(Guid collectionId, List<PseoCollectionNiche> niches);

    /// <summary>
    /// Expands niches × subtopics into the estimated page list for generation.
    /// </summary>
    Task<List<PseoPage>> ExpandSubtopicsAsync(Guid collectionId);
}
