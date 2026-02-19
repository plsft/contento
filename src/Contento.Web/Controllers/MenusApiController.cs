using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Controllers;

/// <summary>
/// REST API for navigation menu management
/// </summary>
[Tags("Menus")]
[ApiController]
[Route("api/v1/menus")]
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
public class MenusApiController : ControllerBase
{
    private readonly IMenuService _menuService;
    private readonly ISiteService _siteService;

    public MenusApiController(IMenuService menuService, ISiteService siteService)
    {
        _menuService = menuService;
        _siteService = siteService;
    }

    [HttpGet]
    [EndpointSummary("List menus")]
    [EndpointDescription("Returns all navigation menus for the current site.")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> List()
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var menus = await _menuService.GetBySiteAsync(siteId);
        return Ok(new { data = menus });
    }

    [HttpGet("{id}")]
    [EndpointSummary("Get a menu with its items")]
    [EndpointDescription("Returns menu details and all items as both a flat list and a rendered tree.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get(string id)
    {
        if (!Guid.TryParse(id, out var menuId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid menu ID." } });

        var menu = await _menuService.GetByIdAsync(menuId);
        if (menu == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Menu not found." } });

        var items = await _menuService.GetItemsAsync(menuId);
        var tree = await _menuService.GetItemTreeAsync(menuId);

        return Ok(new { data = new { menu, items, tree } });
    }

    [HttpPost]
    [EndpointSummary("Create a menu")]
    [EndpointDescription("Creates a new navigation menu for the current site.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateMenuRequest request)
    {
        try
        {
            var siteId = HttpContext.GetCurrentSiteId();

            var menu = new Menu
            {
                SiteId = siteId,
                Name = request.Name ?? "Untitled",
                Slug = request.Slug ?? "",
                Location = request.Location ?? "header",
                IsActive = request.IsActive ?? true
            };

            var created = await _menuService.CreateAsync(menu);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, new { data = created });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpPut("{id}")]
    [EndpointSummary("Update a menu")]
    [EndpointDescription("Updates an existing menu's name, location, or active status.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateMenuRequest request)
    {
        if (!Guid.TryParse(id, out var menuId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid menu ID." } });

        try
        {
            var existing = await _menuService.GetByIdAsync(menuId);
            if (existing == null)
                return NotFound(new { error = new { code = "NOT_FOUND", message = "Menu not found." } });

            if (request.Name != null) existing.Name = request.Name;
            if (request.Slug != null) existing.Slug = request.Slug;
            if (request.Location != null) existing.Location = request.Location;
            if (request.IsActive.HasValue) existing.IsActive = request.IsActive.Value;

            var updated = await _menuService.UpdateAsync(existing);
            return Ok(new { data = updated });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpDelete("{id}")]
    [EndpointSummary("Delete a menu")]
    [EndpointDescription("Permanently deletes a menu and all its items.")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
        if (!Guid.TryParse(id, out var menuId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid menu ID." } });

        var existing = await _menuService.GetByIdAsync(menuId);
        if (existing == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Menu not found." } });

        await _menuService.DeleteAsync(menuId);
        return NoContent();
    }

    // ─── Menu Items ─────────────────────────────────────

    [HttpPost("{id}/items")]
    [EndpointSummary("Add an item to a menu")]
    [EndpointDescription("Adds a new navigation item to the specified menu.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> AddItem(string id, [FromBody] CreateMenuItemRequest request)
    {
        if (!Guid.TryParse(id, out var menuId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid menu ID." } });

        try
        {
            var item = new MenuItem
            {
                MenuId = menuId,
                Label = request.Label ?? "Untitled",
                Url = request.Url,
                LinkType = request.LinkType ?? "custom",
                LinkId = request.LinkId,
                Target = request.Target ?? "_self",
                ParentId = request.ParentId,
                SortOrder = request.SortOrder ?? 0
            };

            var created = await _menuService.AddItemAsync(item);
            return Ok(new { data = created });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpPut("{id}/items/{itemId}")]
    [EndpointSummary("Update a menu item")]
    [EndpointDescription("Updates an existing menu item's label, URL, type, or other properties.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateItem(string id, string itemId, [FromBody] UpdateMenuItemRequest request)
    {
        if (!Guid.TryParse(itemId, out var parsedItemId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid item ID." } });

        try
        {
            var items = await _menuService.GetItemsAsync(Guid.Parse(id));
            var existing = items.FirstOrDefault(i => i.Id == parsedItemId);
            if (existing == null)
                return NotFound(new { error = new { code = "NOT_FOUND", message = "Menu item not found." } });

            if (request.Label != null) existing.Label = request.Label;
            if (request.Url != null) existing.Url = request.Url;
            if (request.LinkType != null) existing.LinkType = request.LinkType;
            if (request.LinkId.HasValue) existing.LinkId = request.LinkId;
            if (request.Target != null) existing.Target = request.Target;
            if (request.ParentId.HasValue) existing.ParentId = request.ParentId;
            if (request.SortOrder.HasValue) existing.SortOrder = request.SortOrder.Value;

            var updated = await _menuService.UpdateItemAsync(existing);
            return Ok(new { data = updated });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpDelete("{id}/items/{itemId}")]
    [EndpointSummary("Remove a menu item")]
    [EndpointDescription("Permanently removes an item from a menu.")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> RemoveItem(string id, string itemId)
    {
        if (!Guid.TryParse(itemId, out var parsedItemId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid item ID." } });

        await _menuService.RemoveItemAsync(parsedItemId);
        return NoContent();
    }

    [HttpPut("{id}/reorder")]
    [EndpointSummary("Reorder menu items")]
    [EndpointDescription("Sets the display order of menu items based on the provided ordered list of item IDs.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Reorder(string id, [FromBody] ReorderRequest request)
    {
        if (!Guid.TryParse(id, out var menuId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid menu ID." } });

        if (request.ItemIds == null || request.ItemIds.Count == 0)
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = "Item IDs are required." } });

        await _menuService.ReorderItemsAsync(menuId, request.ItemIds);
        return Ok(new { data = new { reordered = true } });
    }
}

public class CreateMenuRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Location { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateMenuRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Location { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateMenuItemRequest
{
    public string? Label { get; set; }
    public string? Url { get; set; }
    public string? LinkType { get; set; }
    public Guid? LinkId { get; set; }
    public string? Target { get; set; }
    public Guid? ParentId { get; set; }
    public int? SortOrder { get; set; }
}

public class UpdateMenuItemRequest
{
    public string? Label { get; set; }
    public string? Url { get; set; }
    public string? LinkType { get; set; }
    public Guid? LinkId { get; set; }
    public string? Target { get; set; }
    public Guid? ParentId { get; set; }
    public int? SortOrder { get; set; }
}

public class ReorderRequest
{
    public List<Guid> ItemIds { get; set; } = [];
}
