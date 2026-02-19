using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing navigation menus and their items.
/// Supports CRUD for menus and items, tree rendering, and reordering.
/// </summary>
public interface IMenuService
{
    // ─── Menu CRUD ──────────────────────────────────────

    /// <summary>
    /// Retrieves a menu by its unique identifier.
    /// </summary>
    Task<Menu?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves all menus for a site.
    /// </summary>
    Task<IEnumerable<Menu>> GetBySiteAsync(Guid siteId);

    /// <summary>
    /// Retrieves the active menu assigned to a specific location for a site.
    /// </summary>
    Task<Menu?> GetByLocationAsync(Guid siteId, string location);

    /// <summary>
    /// Creates a new menu.
    /// </summary>
    Task<Menu> CreateAsync(Menu menu);

    /// <summary>
    /// Updates an existing menu.
    /// </summary>
    Task<Menu> UpdateAsync(Menu menu);

    /// <summary>
    /// Deletes a menu and all its items.
    /// </summary>
    Task DeleteAsync(Guid id);

    // ─── MenuItem CRUD ──────────────────────────────────

    /// <summary>
    /// Retrieves all items for a menu as a flat ordered list.
    /// </summary>
    Task<IEnumerable<MenuItem>> GetItemsAsync(Guid menuId);

    /// <summary>
    /// Retrieves all items for a menu as a nested tree with resolved URLs.
    /// </summary>
    Task<IEnumerable<MenuItemNode>> GetItemTreeAsync(Guid menuId);

    /// <summary>
    /// Adds a new item to a menu.
    /// </summary>
    Task<MenuItem> AddItemAsync(MenuItem item);

    /// <summary>
    /// Updates an existing menu item.
    /// </summary>
    Task<MenuItem> UpdateItemAsync(MenuItem item);

    /// <summary>
    /// Removes a menu item by its identifier.
    /// </summary>
    Task RemoveItemAsync(Guid itemId);

    /// <summary>
    /// Reorders items within a menu by setting sort_order based on the position in the list.
    /// </summary>
    Task ReorderItemsAsync(Guid menuId, List<Guid> orderedIds);
}

/// <summary>
/// A menu item with resolved URL and nested children, ready for rendering.
/// </summary>
public class MenuItemNode
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Target { get; set; } = "_self";
    public string? CssClass { get; set; }
    public List<MenuItemNode> Children { get; set; } = [];
}
