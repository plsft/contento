using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Web.Pages.Admin.Themes;

public class IndexModel : PageModel
{
    private readonly IThemeService _themeService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IThemeService themeService, ILogger<IndexModel> logger)
    {
        _themeService = themeService;
        _logger = logger;
    }

    public IEnumerable<Theme> Themes { get; set; } = [];

    public async Task OnGetAsync()
    {
        try
        {
            Themes = await _themeService.GetAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load themes in {Page}", nameof(IndexModel));
        }
    }

    public async Task<IActionResult> OnPostActivateAsync(Guid id)
    {
        try
        {
            await _themeService.ActivateAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate theme in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _themeService.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete theme in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }

    /// <summary>
    /// Extracts preview colors from the CssVariables JSON for display.
    /// Returns a flat dictionary of variable name to color value.
    /// </summary>
    public Dictionary<string, string> GetPreviewColors(string? cssVariablesJson)
    {
        var colors = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(cssVariablesJson)) return colors;

        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(cssVariablesJson);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var val = prop.Value.GetString();
                    if (val != null && (val.StartsWith('#') || val.StartsWith("rgb")))
                    {
                        colors[prop.Name] = val;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse theme CSS variables in {Page}", nameof(IndexModel));
        }

        return colors;
    }
}
