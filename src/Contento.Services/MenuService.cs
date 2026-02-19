using System.Data;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for managing navigation menus and their items.
/// </summary>
public class MenuService : IMenuService
{
    private readonly IDbConnection _db;
    private readonly ILogger<MenuService> _logger;

    public MenuService(IDbConnection db, ILogger<MenuService> logger)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
    }

    // ─── Menu CRUD ──────────────────────────────────────

    /// <inheritdoc />
    public async Task<Menu?> GetByIdAsync(Guid id)
    {
        Guard.Against.Default(id);
        return await _db.GetAsync<Menu>(id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Menu>> GetBySiteAsync(Guid siteId)
    {
        Guard.Against.Default(siteId);

        return await _db.QueryAsync<Menu>(
            "SELECT * FROM menus WHERE site_id = @SiteId ORDER BY location, name",
            new { SiteId = siteId });
    }

    /// <inheritdoc />
    public async Task<Menu?> GetByLocationAsync(Guid siteId, string location)
    {
        Guard.Against.Default(siteId);
        Guard.Against.NullOrWhiteSpace(location);

        var results = await _db.QueryAsync<Menu>(
            "SELECT * FROM menus WHERE site_id = @SiteId AND location = @Location AND is_active = true LIMIT 1",
            new { SiteId = siteId, Location = location });
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<Menu> CreateAsync(Menu menu)
    {
        Guard.Against.Null(menu);
        Guard.Against.NullOrWhiteSpace(menu.Name);
        Guard.Against.Default(menu.SiteId);

        menu.Id = Guid.NewGuid();
        menu.CreatedAt = DateTime.UtcNow;
        menu.UpdatedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(menu.Slug))
            menu.Slug = GenerateSlug(menu.Name);

        if (string.IsNullOrWhiteSpace(menu.Location))
            menu.Location = "header";

        await _db.InsertAsync(menu);
        _logger.LogInformation("Menu created: {MenuName} at {Location}", menu.Name, menu.Location);
        return menu;
    }

    /// <inheritdoc />
    public async Task<Menu> UpdateAsync(Menu menu)
    {
        Guard.Against.Null(menu);
        Guard.Against.Default(menu.Id);

        menu.UpdatedAt = DateTime.UtcNow;
        await _db.UpdateAsync(menu);
        return menu;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        Guard.Against.Default(id);

        var menu = await _db.GetAsync<Menu>(id);
        if (menu == null) return;

        // Delete all menu items first
        await _db.ExecuteAsync(
            "DELETE FROM menu_items WHERE menu_id = @Id",
            new { Id = id });

        await _db.DeleteAsync(menu);
        _logger.LogInformation("Menu deleted: {MenuId}", id);
    }

    // ─── MenuItem CRUD ──────────────────────────────────

    /// <inheritdoc />
    public async Task<IEnumerable<MenuItem>> GetItemsAsync(Guid menuId)
    {
        Guard.Against.Default(menuId);

        return await _db.QueryAsync<MenuItem>(
            "SELECT * FROM menu_items WHERE menu_id = @MenuId ORDER BY sort_order, label",
            new { MenuId = menuId });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MenuItemNode>> GetItemTreeAsync(Guid menuId)
    {
        Guard.Against.Default(menuId);

        var items = (await _db.QueryAsync<MenuItem>(
            "SELECT * FROM menu_items WHERE menu_id = @MenuId AND is_visible = true ORDER BY sort_order, label",
            new { MenuId = menuId })).ToList();

        if (items.Count == 0)
            return [];

        // Resolve URLs for linked items
        await ResolveUrlsAsync(items);

        // Build tree
        return BuildTree(items);
    }

    /// <inheritdoc />
    public async Task<MenuItem> AddItemAsync(MenuItem item)
    {
        Guard.Against.Null(item);
        Guard.Against.NullOrWhiteSpace(item.Label);
        Guard.Against.Default(item.MenuId);

        item.Id = Guid.NewGuid();
        item.CreatedAt = DateTime.UtcNow;

        await _db.InsertAsync(item);
        return item;
    }

    /// <inheritdoc />
    public async Task<MenuItem> UpdateItemAsync(MenuItem item)
    {
        Guard.Against.Null(item);
        Guard.Against.Default(item.Id);

        await _db.UpdateAsync(item);
        return item;
    }

    /// <inheritdoc />
    public async Task RemoveItemAsync(Guid itemId)
    {
        Guard.Against.Default(itemId);

        var item = await _db.GetAsync<MenuItem>(itemId);
        if (item == null) return;

        // Re-parent children to the deleted item's parent
        await _db.ExecuteAsync(
            "UPDATE menu_items SET parent_id = @ParentId WHERE parent_id = @Id",
            new { ParentId = item.ParentId, Id = itemId });

        await _db.DeleteAsync(item);
    }

    /// <inheritdoc />
    public async Task ReorderItemsAsync(Guid menuId, List<Guid> orderedIds)
    {
        Guard.Against.Default(menuId);
        Guard.Against.Null(orderedIds);

        for (var i = 0; i < orderedIds.Count; i++)
        {
            await _db.ExecuteAsync(
                "UPDATE menu_items SET sort_order = @SortOrder WHERE id = @Id AND menu_id = @MenuId",
                new { SortOrder = i, Id = orderedIds[i], MenuId = menuId });
        }
    }

    // ─── Private helpers ────────────────────────────────

    private async Task ResolveUrlsAsync(List<MenuItem> items)
    {
        var postIds = items.Where(i => i.LinkType == "post" && i.LinkId.HasValue).Select(i => i.LinkId!.Value).Distinct().ToList();
        var categoryIds = items.Where(i => i.LinkType == "category" && i.LinkId.HasValue).Select(i => i.LinkId!.Value).Distinct().ToList();

        var postSlugs = new Dictionary<Guid, string>();
        var categorySlugs = new Dictionary<Guid, string>();

        if (postIds.Count > 0)
        {
            var posts = await _db.QueryAsync<Post>(
                "SELECT id, slug FROM posts WHERE id = ANY(@Ids)",
                new { Ids = postIds.ToArray() });
            foreach (var p in posts)
                postSlugs[p.Id] = p.Slug;
        }

        if (categoryIds.Count > 0)
        {
            var categories = await _db.QueryAsync<Category>(
                "SELECT id, slug FROM categories WHERE id = ANY(@Ids)",
                new { Ids = categoryIds.ToArray() });
            foreach (var c in categories)
                categorySlugs[c.Id] = c.Slug;
        }

        foreach (var item in items)
        {
            item.Url = item.LinkType switch
            {
                "post" when item.LinkId.HasValue && postSlugs.TryGetValue(item.LinkId.Value, out var ps)
                    => $"/{ps}",
                "category" when item.LinkId.HasValue && categorySlugs.TryGetValue(item.LinkId.Value, out var cs)
                    => $"/category/{cs}",
                "tag" => $"/tag/{item.Url}",
                _ => item.Url ?? "#"
            };
        }
    }

    private static List<MenuItemNode> BuildTree(List<MenuItem> items)
    {
        var lookup = items.ToLookup(i => i.ParentId);

        List<MenuItemNode> BuildChildren(Guid? parentId)
        {
            return lookup[parentId]
                .Select(i => new MenuItemNode
                {
                    Id = i.Id,
                    Label = i.Label,
                    Url = i.Url ?? "#",
                    Target = i.Target,
                    CssClass = i.CssClass,
                    Children = BuildChildren(i.Id)
                })
                .ToList();
        }

        return BuildChildren(null);
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant().Trim();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[\s-]+", "-");
        slug = slug.Trim('-');
        return string.IsNullOrEmpty(slug) ? "menu" : slug;
    }
}
