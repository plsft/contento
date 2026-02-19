using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Plugins;

public class IndexModel : PageModel
{
    private readonly IPluginService _pluginService;
    private readonly ISiteService _siteService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IPluginService pluginService, ISiteService siteService, ILogger<IndexModel> logger)
    {
        _pluginService = pluginService;
        _siteService = siteService;
        _logger = logger;
    }

    public IEnumerable<InstalledPlugin> Plugins { get; set; } = [];
    public int TotalCount { get; set; }
    public int EnabledCount { get; set; }

    public async Task OnGetAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();

        try
        {
            Plugins = await _pluginService.GetAllBySiteAsync(siteId);
            TotalCount = await _pluginService.GetTotalCountAsync(siteId);
            EnabledCount = await _pluginService.GetTotalCountAsync(siteId, enabledOnly: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugins in {Page}", nameof(IndexModel));
        }
    }

    public async Task<IActionResult> OnPostEnableAsync(Guid id)
    {
        try
        {
            await _pluginService.EnableAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable plugin in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDisableAsync(Guid id)
    {
        try
        {
            await _pluginService.DisableAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable plugin in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUninstallAsync(Guid id)
    {
        try
        {
            await _pluginService.UninstallAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall plugin in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }
}
