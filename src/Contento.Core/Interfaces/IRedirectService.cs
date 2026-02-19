using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing URL redirects (301/302).
/// Supports CRUD operations, path-based lookup for middleware,
/// hit tracking, and automatic redirect creation on slug changes.
/// </summary>
public interface IRedirectService
{
    /// <summary>
    /// Retrieves a redirect by its unique identifier.
    /// </summary>
    /// <param name="id">The redirect identifier.</param>
    /// <returns>The redirect if found; otherwise null.</returns>
    Task<Redirect?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves all redirects for a site, ordered by creation date descending.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A paginated collection of redirects.</returns>
    Task<IEnumerable<Redirect>> GetAllAsync(Guid siteId, int page = 1, int pageSize = 50);

    /// <summary>
    /// Retrieves an active redirect matching a specific source path for a site.
    /// Used by the redirect middleware for path-based lookup.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="fromPath">The source path to match.</param>
    /// <returns>The active redirect if found; otherwise null.</returns>
    Task<Redirect?> GetByFromPathAsync(Guid siteId, string fromPath);

    /// <summary>
    /// Creates a new redirect.
    /// </summary>
    /// <param name="redirect">The redirect to create.</param>
    /// <returns>The created redirect with generated identifier.</returns>
    Task<Redirect> CreateAsync(Redirect redirect);

    /// <summary>
    /// Updates an existing redirect.
    /// </summary>
    /// <param name="redirect">The redirect with updated fields.</param>
    /// <returns>The updated redirect.</returns>
    Task<Redirect> UpdateAsync(Redirect redirect);

    /// <summary>
    /// Deletes a redirect by its identifier.
    /// </summary>
    /// <param name="id">The redirect identifier.</param>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Increments the hit count and updates the last-hit timestamp for a redirect.
    /// </summary>
    /// <param name="id">The redirect identifier.</param>
    Task IncrementHitCountAsync(Guid id);

    /// <summary>
    /// Returns the total count of redirects for a site.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <returns>The total count of redirects.</returns>
    Task<int> GetTotalCountAsync(Guid siteId);

    /// <summary>
    /// Auto-creates a 301 redirect when a post slug changes.
    /// If a redirect from the old slug already exists, it is not duplicated.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="oldSlug">The previous slug (without leading slash).</param>
    /// <param name="newSlug">The new slug (without leading slash).</param>
    Task CreateSlugChangeRedirectAsync(Guid siteId, string oldSlug, string newSlug);
}
