using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Settings;

public class AiModel : PageModel
{
    private readonly ISiteService _siteService;
    private readonly ILogger<AiModel> _logger;

    public AiModel(ISiteService siteService, ILogger<AiModel> logger)
    {
        _siteService = siteService;
        _logger = logger;
    }

    [BindProperty]
    public string Provider { get; set; } = "openai";

    [BindProperty]
    public string Model { get; set; } = "openai/gpt-4o-mini";

    [BindProperty]
    public string ApiKey { get; set; } = "";

    [BindProperty]
    public bool Enabled { get; set; }

    public string MaskedKey { get; set; } = "";
    public bool SaveSuccess { get; set; }

    public void OnGet()
    {
        var settings = LoadAiSettings();
        if (settings != null)
        {
            Provider = settings.Provider;
            Model = settings.Model;
            Enabled = settings.Enabled;
            if (!string.IsNullOrEmpty(settings.ApiKey))
                MaskedKey = "****" + settings.ApiKey[^Math.Min(4, settings.ApiKey.Length)..];
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var site = HttpContext.GetCurrentSite();

        // Load existing settings
        var existingSettings = new Dictionary<string, JsonElement>();
        if (!string.IsNullOrWhiteSpace(site.Settings) && site.Settings != "{}")
        {
            var doc = JsonDocument.Parse(site.Settings);
            foreach (var prop in doc.RootElement.EnumerateObject())
                existingSettings[prop.Name] = prop.Value.Clone();
        }

        // If API key is empty and we already have one, keep the existing key
        var currentAi = LoadAiSettings();
        var apiKey = ApiKey;
        if (string.IsNullOrEmpty(apiKey) && currentAi != null)
            apiKey = currentAi.ApiKey;

        var aiSettings = new AiSettings
        {
            Provider = Provider,
            Model = Model,
            ApiKey = apiKey,
            Enabled = Enabled
        };

        // Merge ai settings into site settings
        var aiJson = JsonSerializer.SerializeToElement(aiSettings, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        existingSettings["ai"] = aiJson;

        var newSettingsJson = JsonSerializer.Serialize(existingSettings);
        await _siteService.UpdateSettingsAsync(site.Id, newSettingsJson);

        SaveSuccess = true;
        if (!string.IsNullOrEmpty(apiKey))
            MaskedKey = "****" + apiKey[^Math.Min(4, apiKey.Length)..];

        return Page();
    }

    private AiSettings? LoadAiSettings()
    {
        var site = HttpContext.TryGetCurrentSite();
        if (site == null || string.IsNullOrWhiteSpace(site.Settings) || site.Settings == "{}")
            return null;

        try
        {
            var doc = JsonDocument.Parse(site.Settings);
            if (doc.RootElement.TryGetProperty("ai", out var aiElement))
            {
                return JsonSerializer.Deserialize<AiSettings>(aiElement.GetRawText(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse AI settings in {Page}", nameof(AiModel));
        }

        return null;
    }
}
