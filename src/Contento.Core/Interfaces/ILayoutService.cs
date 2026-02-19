using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing page layout templates.
/// Handles CRUD operations, component management, and default layout assignment.
/// </summary>
public interface ILayoutService
{
    /// <summary>
    /// Retrieves a layout by its unique identifier.
    /// </summary>
    /// <param name="id">The layout identifier.</param>
    /// <returns>The layout if found; otherwise null.</returns>
    Task<Layout?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves a layout by its slug within a specific site.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="slug">The URL-friendly slug.</param>
    /// <returns>The layout if found; otherwise null.</returns>
    Task<Layout?> GetBySlugAsync(Guid siteId, string slug);

    /// <summary>
    /// Retrieves a layout along with all its associated layout components.
    /// </summary>
    /// <param name="id">The layout identifier.</param>
    /// <returns>A tuple of the layout and its components, or null if the layout is not found.</returns>
    Task<(Layout Layout, IEnumerable<LayoutComponent> Components)?> GetWithComponentsAsync(Guid id);

    /// <summary>
    /// Retrieves all layouts for a site with pagination.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A paginated collection of layouts.</returns>
    Task<IEnumerable<Layout>> GetAllBySiteAsync(Guid siteId, int page = 1, int pageSize = 50);

    /// <summary>
    /// Retrieves the default layout for a site.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <returns>The default layout if one is set; otherwise null.</returns>
    Task<Layout?> GetDefaultAsync(Guid siteId);

    /// <summary>
    /// Creates a new layout within a site.
    /// </summary>
    /// <param name="layout">The layout to create.</param>
    /// <returns>The created layout with generated identifier.</returns>
    Task<Layout> CreateAsync(Layout layout);

    /// <summary>
    /// Updates an existing layout.
    /// </summary>
    /// <param name="layout">The layout with updated fields.</param>
    /// <returns>The updated layout.</returns>
    Task<Layout> UpdateAsync(Layout layout);

    /// <summary>
    /// Deletes a layout. Posts using this layout will fall back to the site's default layout.
    /// Cannot delete the site's default layout.
    /// </summary>
    /// <param name="id">The layout identifier.</param>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Sets a layout as the default for its site. The previously default layout
    /// will have its is_default flag cleared.
    /// </summary>
    /// <param name="id">The layout identifier to set as default.</param>
    Task SetDefaultAsync(Guid id);

    /// <summary>
    /// Retrieves all components for a specific layout, ordered by region and sort order.
    /// </summary>
    /// <param name="layoutId">The layout identifier.</param>
    /// <returns>A collection of layout components.</returns>
    Task<IEnumerable<LayoutComponent>> GetComponentsAsync(Guid layoutId);

    /// <summary>
    /// Adds a component to a layout.
    /// </summary>
    /// <param name="component">The layout component to add.</param>
    /// <returns>The created layout component with generated identifier.</returns>
    Task<LayoutComponent> AddComponentAsync(LayoutComponent component);

    /// <summary>
    /// Updates an existing layout component.
    /// </summary>
    /// <param name="component">The component with updated fields.</param>
    /// <returns>The updated layout component.</returns>
    Task<LayoutComponent> UpdateComponentAsync(LayoutComponent component);

    /// <summary>
    /// Removes a component from a layout.
    /// </summary>
    /// <param name="componentId">The layout component identifier.</param>
    Task RemoveComponentAsync(Guid componentId);
}
