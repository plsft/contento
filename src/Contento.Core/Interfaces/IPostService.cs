using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing blog posts, including CRUD, publishing, and versioning.
/// </summary>
public interface IPostService
{
    /// <summary>
    /// Retrieves a post by its unique identifier.
    /// </summary>
    /// <param name="id">The post identifier.</param>
    /// <returns>The post if found; otherwise null.</returns>
    Task<Post?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves a post by site and slug combination.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="slug">The URL-friendly slug.</param>
    /// <returns>The post if found; otherwise null.</returns>
    Task<Post?> GetBySlugAsync(Guid siteId, string slug);

    /// <summary>
    /// Creates a new post with auto-generated slug, word count, and reading time.
    /// </summary>
    /// <param name="post">The post to create.</param>
    /// <returns>The created post with generated fields populated.</returns>
    Task<Post> CreateAsync(Post post);

    /// <summary>
    /// Updates an existing post. Automatically creates a version snapshot, recalculates
    /// word count and reading time, and updates the timestamp.
    /// </summary>
    /// <param name="post">The post with updated fields.</param>
    /// <param name="changedBy">The user performing the update.</param>
    /// <param name="changeSummary">Optional summary of changes for the version record.</param>
    /// <returns>The updated post.</returns>
    Task<Post> UpdateAsync(Post post, Guid changedBy, string? changeSummary = null);

    /// <summary>
    /// Soft-deletes a post by setting its status to archived.
    /// </summary>
    /// <param name="id">The post identifier.</param>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Publishes a post by setting its status to published and recording the publish timestamp.
    /// </summary>
    /// <param name="id">The post identifier.</param>
    Task PublishAsync(Guid id);

    /// <summary>
    /// Reverts a published post back to draft status.
    /// </summary>
    /// <param name="id">The post identifier.</param>
    Task UnpublishAsync(Guid id);

    /// <summary>
    /// Retrieves a filtered, paginated list of posts.
    /// </summary>
    /// <param name="siteId">Required site scope.</param>
    /// <param name="status">Optional status filter (draft, published, archived, etc.).</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <param name="tag">Optional tag filter.</param>
    /// <param name="search">Optional full-text search query.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of results per page.</param>
    /// <returns>A list of posts matching the criteria.</returns>
    Task<IEnumerable<Post>> GetAllAsync(Guid siteId, string? status = null, Guid? categoryId = null,
        string? tag = null, string? search = null, int page = 1, int pageSize = 20);

    /// <summary>
    /// Returns the total count of posts matching the specified filters, for pagination.
    /// </summary>
    Task<int> GetTotalCountAsync(Guid siteId, string? status = null, Guid? categoryId = null,
        string? tag = null, string? search = null);
}
