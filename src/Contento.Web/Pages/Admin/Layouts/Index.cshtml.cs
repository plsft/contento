using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Layouts;

public class IndexModel : PageModel
{
    private readonly ILayoutService _layoutService;
    private readonly ISiteService _siteService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILayoutService layoutService, ISiteService siteService, ILogger<IndexModel> logger)
    {
        _layoutService = layoutService;
        _siteService = siteService;
        _logger = logger;
    }

    public IEnumerable<Layout> Layouts { get; set; } = [];

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string? Description { get; set; }

    public async Task OnGetAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();

        try
        {
            Layouts = await _layoutService.GetAllBySiteAsync(siteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load layouts in {Page}", nameof(IndexModel));
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();

        try
        {
            var layout = new Layout
            {
                SiteId = siteId,
                Name = Name,
                Slug = GenerateSlug(Name),
                Description = Description
            };
            await _layoutService.CreateAsync(layout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create layout in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSetDefaultAsync(Guid id)
    {
        try
        {
            await _layoutService.SetDefaultAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set default layout in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _layoutService.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete layout in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("--", "-")
            .Trim('-');
    }
}
