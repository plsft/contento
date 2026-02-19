using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Menus;

public class EditModel : PageModel
{
    private readonly IMenuService _menuService;
    private readonly IPostService _postService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        IMenuService menuService,
        IPostService postService,
        ICategoryService categoryService,
        ILogger<EditModel> logger)
    {
        _menuService = menuService;
        _postService = postService;
        _categoryService = categoryService;
        _logger = logger;
    }

    public Menu Menu { get; set; } = null!;
    public List<MenuItem> Items { get; set; } = [];
    public List<Post> Posts { get; set; } = [];
    public List<Category> Categories { get; set; } = [];

    // ─── Menu properties ────────────────────────────────

    [BindProperty]
    public string MenuName { get; set; } = string.Empty;

    [BindProperty]
    public string MenuLocation { get; set; } = string.Empty;

    [BindProperty]
    public bool MenuIsActive { get; set; } = true;

    // ─── Item properties ────────────────────────────────

    [BindProperty]
    public string Label { get; set; } = string.Empty;

    [BindProperty]
    public new string? Url { get; set; }

    [BindProperty]
    public string LinkType { get; set; } = "custom";

    [BindProperty]
    public Guid? LinkId { get; set; }

    [BindProperty]
    public string Target { get; set; } = "_self";

    [BindProperty]
    public Guid? ParentId { get; set; }

    [BindProperty]
    public Guid? EditItemId { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var menu = await _menuService.GetByIdAsync(id);
            if (menu == null) return RedirectToPage("Index");

            Menu = menu;
            MenuName = menu.Name;
            MenuLocation = menu.Location;
            MenuIsActive = menu.IsActive;
            Items = (await _menuService.GetItemsAsync(id)).ToList();

            var siteId = HttpContext.GetCurrentSiteId();
            Posts = (await _postService.GetAllAsync(siteId, "published", null, null, null, 1, 200)).ToList();
            Categories = (await _categoryService.GetAllBySiteAsync(siteId)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load menu in {Page}", nameof(EditModel));
            return RedirectToPage("Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateMenuAsync(Guid id)
    {
        try
        {
            var menu = await _menuService.GetByIdAsync(id);
            if (menu != null)
            {
                menu.Name = MenuName;
                menu.Location = MenuLocation;
                menu.IsActive = MenuIsActive;
                await _menuService.UpdateAsync(menu);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update menu in {Page}", nameof(EditModel));
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddItemAsync(Guid id)
    {
        try
        {
            var item = new MenuItem
            {
                MenuId = id,
                Label = Label,
                Url = Url,
                LinkType = LinkType,
                LinkId = LinkId,
                Target = Target,
                ParentId = ParentId,
                SortOrder = (await _menuService.GetItemsAsync(id)).Count()
            };
            await _menuService.AddItemAsync(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add menu item in {Page}", nameof(EditModel));
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUpdateItemAsync(Guid id)
    {
        if (EditItemId == null) return RedirectToPage(new { id });

        try
        {
            var items = await _menuService.GetItemsAsync(id);
            var item = items.FirstOrDefault(i => i.Id == EditItemId.Value);
            if (item != null)
            {
                item.Label = Label;
                item.Url = Url;
                item.LinkType = LinkType;
                item.LinkId = LinkId;
                item.Target = Target;
                item.ParentId = ParentId;
                await _menuService.UpdateItemAsync(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update menu item in {Page}", nameof(EditModel));
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteItemAsync(Guid id, Guid itemId)
    {
        try
        {
            await _menuService.RemoveItemAsync(itemId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete menu item in {Page}", nameof(EditModel));
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostMoveUpAsync(Guid id, Guid itemId)
    {
        try
        {
            var items = (await _menuService.GetItemsAsync(id)).ToList();
            var ids = items.Select(i => i.Id).ToList();
            var index = ids.IndexOf(itemId);
            if (index > 0)
            {
                ids.RemoveAt(index);
                ids.Insert(index - 1, itemId);
                await _menuService.ReorderItemsAsync(id, ids);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move menu item in {Page}", nameof(EditModel));
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostMoveDownAsync(Guid id, Guid itemId)
    {
        try
        {
            var items = (await _menuService.GetItemsAsync(id)).ToList();
            var ids = items.Select(i => i.Id).ToList();
            var index = ids.IndexOf(itemId);
            if (index >= 0 && index < ids.Count - 1)
            {
                ids.RemoveAt(index);
                ids.Insert(index + 1, itemId);
                await _menuService.ReorderItemsAsync(id, ids);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move menu item in {Page}", nameof(EditModel));
        }

        return RedirectToPage(new { id });
    }
}
