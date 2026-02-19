using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Web.Pages.Admin.Plugins;

public class SettingsModel : PageModel
{
    private readonly IPluginService _pluginService;
    private readonly ILogger<SettingsModel> _logger;

    public SettingsModel(IPluginService pluginService, ILogger<SettingsModel> logger)
    {
        _pluginService = pluginService;
        _logger = logger;
    }

    public InstalledPlugin? Plugin { get; set; }

    [BindProperty]
    public string SettingsJson { get; set; } = "{}";

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (!Guid.TryParse(id, out var pluginId))
            return NotFound();

        try
        {
            Plugin = await _pluginService.GetByIdAsync(pluginId);
            if (Plugin == null)
                return NotFound();

            SettingsJson = await _pluginService.GetSettingsAsync(pluginId);
            if (string.IsNullOrWhiteSpace(SettingsJson))
                SettingsJson = "{}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin settings in {Page}", nameof(SettingsModel));
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        if (!Guid.TryParse(id, out var pluginId))
            return NotFound();

        try
        {
            await _pluginService.UpdateSettingsAsync(pluginId, SettingsJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save plugin settings in {Page}", nameof(SettingsModel));
        }

        return RedirectToPage(new { id });
    }
}
