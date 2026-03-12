using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing individual pSEO pages — CRUD, status tracking, and bulk operations.
/// </summary>
public interface IPseoPageService
{
    /// <summary>
    /// Retrieves a page by its unique identifier.
    /// </summary>
    Task<PseoPage?> GetByIdAsync(Guid id);

    /// <summary>
    /// Returns a paginated list of pages within a collection, optionally filtered by status.
    /// </summary>
    Task<List<PseoPage>> GetByCollectionIdAsync(Guid collectionId, string? status, int page, int pageSize);

    /// <summary>
    /// Returns a paginated list of pages within a project, optionally filtered by status.
    /// </summary>
    Task<List<PseoPage>> GetByProjectIdAsync(Guid projectId, string? status, int page, int pageSize);

    /// <summary>
    /// Retrieves a page by project and slug combination.
    /// </summary>
    Task<PseoPage?> GetBySlugAsync(Guid projectId, string slug);

    /// <summary>
    /// Creates a new pSEO page.
    /// </summary>
    Task<PseoPage> CreateAsync(PseoPage page);

    /// <summary>
    /// Updates an existing pSEO page.
    /// </summary>
    Task<PseoPage> UpdateAsync(PseoPage page);

    /// <summary>
    /// Deletes a pSEO page.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Updates the status and optional validation errors for a page.
    /// </summary>
    Task UpdateStatusAsync(Guid id, string status, string? validationErrors);

    /// <summary>
    /// Creates multiple pages in a single batch operation.
    /// </summary>
    Task<List<PseoPage>> BulkCreateAsync(List<PseoPage> pages);

    /// <summary>
    /// Returns all pages in a collection that have a failed status.
    /// </summary>
    Task<List<PseoPage>> GetFailedPagesAsync(Guid collectionId);

    /// <summary>
    /// Returns the next batch of pages pending publish within a collection.
    /// </summary>
    Task<List<PseoPage>> GetPendingPublishAsync(Guid collectionId, int batchSize);
}
