using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Menus;

public class IndexModel : PageModel
{
    private readonly IMenuService _menuService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IMenuService menuService, ILogger<IndexModel> logger)
    {
        _menuService = menuService;
        _logger = logger;
    }

    public List<MenuViewModel> Menus { get; set; } = [];

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string Location { get; set; } = "header";

    [BindProperty]
    public bool IsActive { get; set; } = true;

    public async Task OnGetAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();

        try
        {
            var menus = await _menuService.GetBySiteAsync(siteId);
            foreach (var menu in menus)
            {
                var items = await _menuService.GetItemsAsync(menu.Id);
                Menus.Add(new MenuViewModel
                {
                    Menu = menu,
                    ItemCount = items.Count()
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load menus in {Page}", nameof(IndexModel));
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();

        try
        {
            var menu = new Menu
            {
                SiteId = siteId,
                Name = Name,
                Location = Location,
                IsActive = IsActive
            };
            await _menuService.CreateAsync(menu);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create menu in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _menuService.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete menu in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }

    public class MenuViewModel
    {
        public Menu Menu { get; set; } = null!;
        public int ItemCount { get; set; }
    }
}
