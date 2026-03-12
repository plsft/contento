using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Settings;

public class IndexModel : PageModel
{
    private readonly ISiteService _siteService;
    private readonly ISpamService _spamService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ISiteService siteService, ISpamService spamService, ILogger<IndexModel> logger)
    {
        _siteService = siteService;
        _spamService = spamService;
        _logger = logger;
    }

    public Site? Site { get; set; }

    [BindProperty]
    public string SiteName { get; set; } = string.Empty;

    [BindProperty]
    public string? Tagline { get; set; }

    [BindProperty]
    public string? Domain { get; set; }

    [BindProperty]
    public string Locale { get; set; } = "en-US";

    [BindProperty]
    public string Timezone { get; set; } = "UTC";

    [BindProperty]
    public int SpamThreshold { get; set; } = 30;

    public SpamStats? SpamStats { get; set; }

    public bool SaveSuccess { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Site = HttpContext.GetCurrentSite();
            SiteName = Site.Name;
            Tagline = Site.Tagline;
            Domain = Site.Domain;
            Locale = Site.Locale;
            Timezone = Site.Timezone;

            SpamStats = await _spamService.GetStatsAsync(Site.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load site settings in {Page}", nameof(IndexModel));
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var site = HttpContext.GetCurrentSite();
            site.Name = SiteName;
            site.Tagline = Tagline;
            site.Domain = Domain;
            site.Locale = Locale;
            site.Timezone = Timezone;
            await _siteService.UpdateAsync(site);
            SaveSuccess = true;

            Site = site;
            SpamStats = await _spamService.GetStatsAsync(site.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save site settings in {Page}", nameof(IndexModel));
        }

        return Page();
    }
}
