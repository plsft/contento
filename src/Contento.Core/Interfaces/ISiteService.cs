using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing Contento site instances.
/// Handles site configuration retrieval, updates, and lookup by slug or domain.
/// </summary>
public interface ISiteService
{
    /// <summary>
    /// Retrieves a site by its unique identifier.
    /// </summary>
    /// <param name="id">The site identifier.</param>
    /// <returns>The site if found; otherwise null.</returns>
    Task<Site?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves a site by its URL-friendly slug.
    /// </summary>
    /// <param name="slug">The site slug.</param>
    /// <returns>The site if found; otherwise null.</returns>
    Task<Site?> GetBySlugAsync(string slug);

    /// <summary>
    /// Retrieves a site by its custom domain name.
    /// </summary>
    /// <param name="domain">The domain name (e.g., "myblog.com").</param>
    /// <returns>The site if found; otherwise null.</returns>
    Task<Site?> GetByDomainAsync(string domain);

    /// <summary>
    /// Retrieves all sites with pagination.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A paginated collection of sites.</returns>
    Task<IEnumerable<Site>> GetAllAsync(int page = 1, int pageSize = 50);

    /// <summary>
    /// Creates a new site.
    /// </summary>
    /// <param name="site">The site to create.</param>
    /// <returns>The created site with generated identifier.</returns>
    Task<Site> CreateAsync(Site site);

    /// <summary>
    /// Updates an existing site's configuration.
    /// </summary>
    /// <param name="site">The site with updated fields.</param>
    /// <returns>The updated site.</returns>
    Task<Site> UpdateAsync(Site site);

    /// <summary>
    /// Deletes a site. This is a destructive operation that cascades to all
    /// related content (posts, categories, media, etc.).
    /// </summary>
    /// <param name="id">The site identifier.</param>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Updates the settings JSON for a site.
    /// </summary>
    /// <param name="id">The site identifier.</param>
    /// <param name="settingsJson">The settings as a JSON string.</param>
    Task UpdateSettingsAsync(Guid id, string settingsJson);

    /// <summary>
    /// Sets the active theme for a site.
    /// </summary>
    /// <param name="id">The site identifier.</param>
    /// <param name="themeId">The theme identifier to assign.</param>
    Task SetThemeAsync(Guid id, Guid themeId);
}
