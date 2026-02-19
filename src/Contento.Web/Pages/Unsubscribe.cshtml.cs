using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Contento.Core.Interfaces;
using Contento.Web.Middleware;

namespace Contento.Web.Pages;

[AllowAnonymous]
public class UnsubscribeModel : PageModel
{
    private readonly INewsletterService _newsletterService;
    private readonly ISiteService _siteService;

    public UnsubscribeModel(INewsletterService newsletterService, ISiteService siteService)
    {
        _newsletterService = newsletterService;
        _siteService = siteService;
    }

    public string SiteName { get; set; } = "Contento";
    public string SiteTagline { get; set; } = "";
    public bool Success { get; set; }
    public string Message { get; set; } = "";

    public async Task OnGetAsync([FromQuery] string? token)
    {
        var site = HttpContext.GetCurrentSite();
        SiteName = site.Name;
        SiteTagline = site.Tagline ?? "";

        if (string.IsNullOrWhiteSpace(token))
        {
            Message = "Invalid unsubscribe link.";
            return;
        }

        var result = await _newsletterService.UnsubscribeAsync(token);
        Success = result;
        Message = result
            ? "You have been successfully unsubscribed. You will no longer receive emails from us."
            : "This unsubscribe link is invalid or has already been used.";
    }
}
