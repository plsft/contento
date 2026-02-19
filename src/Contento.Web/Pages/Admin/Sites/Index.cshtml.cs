using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Web.Pages.Admin.Sites;

public class IndexModel : PageModel
{
    private readonly ISiteService _siteService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ISiteService siteService, ILogger<IndexModel> logger)
    {
        _siteService = siteService;
        _logger = logger;
    }

    public IEnumerable<Site> Sites { get; set; } = [];

    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string SiteSlug { get; set; } = "";
    [BindProperty] public string? Domain { get; set; }

    public async Task OnGetAsync()
    {
        Sites = await _siteService.GetAllAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            var site = new Site
            {
                Name = Name,
                Slug = SiteSlug,
                Domain = Domain
            };
            await _siteService.CreateAsync(site);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create site");
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSetPrimaryAsync(Guid id)
    {
        try
        {
            // Clear all primary flags, then set the selected one
            var sites = await _siteService.GetAllAsync();
            foreach (var s in sites)
            {
                if (s.IsPrimary && s.Id != id)
                {
                    s.IsPrimary = false;
                    await _siteService.UpdateAsync(s);
                }
            }
            var site = await _siteService.GetByIdAsync(id);
            if (site != null)
            {
                site.IsPrimary = true;
                await _siteService.UpdateAsync(site);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set primary site");
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _siteService.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete site");
        }
        return RedirectToPage();
    }
}
