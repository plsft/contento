using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing hierarchical content categories.
/// Supports CRUD operations, tree loading, and site-scoped listing.
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Retrieves a category by its unique identifier.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <returns>The category if found; otherwise null.</returns>
    Task<Category?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves a category by its slug within a specific site.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="slug">The URL-friendly slug.</param>
    /// <returns>The category if found; otherwise null.</returns>
    Task<Category?> GetBySlugAsync(Guid siteId, string slug);

    /// <summary>
    /// Retrieves all categories for a site as a flat list, ordered by sort order.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A paginated collection of categories.</returns>
    Task<IEnumerable<Category>> GetAllBySiteAsync(Guid siteId, int page = 1, int pageSize = 100);

    /// <summary>
    /// Retrieves the full hierarchical category tree for a site.
    /// Returns top-level categories with their children nested recursively.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <returns>A collection of root-level categories with nested children.</returns>
    Task<IEnumerable<Category>> GetTreeAsync(Guid siteId);

    /// <summary>
    /// Retrieves all child categories of a given parent category.
    /// </summary>
    /// <param name="parentId">The parent category identifier.</param>
    /// <returns>A collection of child categories.</returns>
    Task<IEnumerable<Category>> GetChildrenAsync(Guid parentId);

    /// <summary>
    /// Creates a new category within a site.
    /// </summary>
    /// <param name="category">The category to create.</param>
    /// <returns>The created category with generated identifier.</returns>
    Task<Category> CreateAsync(Category category);

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="category">The category with updated fields.</param>
    /// <returns>The updated category.</returns>
    Task<Category> UpdateAsync(Category category);

    /// <summary>
    /// Deletes a category. Posts assigned to this category will have their
    /// category_id set to null. Child categories are re-parented to the deleted category's parent.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Returns the total count of categories for a site.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <returns>The total count of categories.</returns>
    Task<int> GetTotalCountAsync(Guid siteId);
}
