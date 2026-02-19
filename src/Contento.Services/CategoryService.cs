using System.Data;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for managing hierarchical content categories with CRUD, tree building,
/// and site-scoped listing.
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly IDbConnection _db;
    private readonly ILogger<CategoryService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="CategoryService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="logger">The logger.</param>
    public CategoryService(IDbConnection db, ILogger<CategoryService> logger)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task<Category?> GetByIdAsync(Guid id)
    {
        Guard.Against.Default(id);
        return await _db.GetAsync<Category>(id);
    }

    /// <inheritdoc />
    public async Task<Category?> GetBySlugAsync(Guid siteId, string slug)
    {
        Guard.Against.Default(siteId);
        Guard.Against.NullOrWhiteSpace(slug);

        var results = await _db.QueryAsync<Category>(
            "SELECT * FROM categories WHERE site_id = @SiteId AND slug = @Slug LIMIT 1",
            new { SiteId = siteId, Slug = slug });
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Category>> GetAllBySiteAsync(Guid siteId, int page = 1, int pageSize = 100)
    {
        Guard.Against.Default(siteId);

        var offset = (Math.Max(page, 1) - 1) * pageSize;
        return await _db.QueryAsync<Category>(
            "SELECT * FROM categories WHERE site_id = @SiteId ORDER BY sort_order, name LIMIT @Limit OFFSET @Offset",
            new { SiteId = siteId, Limit = pageSize, Offset = offset });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Category>> GetTreeAsync(Guid siteId)
    {
        Guard.Against.Default(siteId);

        var all = await _db.QueryAsync<Category>(
            "SELECT * FROM categories WHERE site_id = @SiteId ORDER BY sort_order, name",
            new { SiteId = siteId });

        var list = all.ToList();
        var lookup = list.ToLookup(c => c.ParentId);

        // Return root categories; caller can use lookup to navigate children
        // For now return flat list; the tree is built in-memory by the caller
        return list;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Category>> GetChildrenAsync(Guid parentId)
    {
        Guard.Against.Default(parentId);

        return await _db.QueryAsync<Category>(
            "SELECT * FROM categories WHERE parent_id = @ParentId ORDER BY sort_order, name",
            new { ParentId = parentId });
    }

    /// <inheritdoc />
    public async Task<Category> CreateAsync(Category category)
    {
        Guard.Against.Null(category);
        Guard.Against.NullOrWhiteSpace(category.Name);
        Guard.Against.Default(category.SiteId);

        category.Id = Guid.NewGuid();
        category.CreatedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(category.Slug))
            category.Slug = GenerateSlug(category.Name);

        await _db.InsertAsync(category);
        return category;
    }

    /// <inheritdoc />
    public async Task<Category> UpdateAsync(Category category)
    {
        Guard.Against.Null(category);
        Guard.Against.Default(category.Id);

        await _db.UpdateAsync(category);
        return category;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        Guard.Against.Default(id);

        var category = await _db.GetAsync<Category>(id);
        if (category == null)
            return;

        // Re-parent child categories to the deleted category's parent
        await _db.ExecuteAsync(
            "UPDATE categories SET parent_id = @ParentId WHERE parent_id = @Id",
            new { ParentId = category.ParentId, Id = id });

        // Nullify category reference on posts
        await _db.ExecuteAsync(
            "UPDATE posts SET category_id = NULL WHERE category_id = @Id",
            new { Id = id });

        await _db.DeleteAsync(category);
    }

    /// <inheritdoc />
    public async Task<int> GetTotalCountAsync(Guid siteId)
    {
        Guard.Against.Default(siteId);

        return await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM categories WHERE site_id = @SiteId",
            new { SiteId = siteId });
    }

    /// <summary>
    /// Generates a URL-friendly slug from a category name.
    /// </summary>
    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant().Trim();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[\s-]+", "-");
        slug = slug.Trim('-');
        return string.IsNullOrEmpty(slug) ? "category" : slug;
    }
}
