using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages;

[AllowAnonymous]
public class MembershipModel : PageModel
{
    private readonly ISiteService _siteService;
    private readonly IMembershipPlanService _membershipPlanService;
    private readonly IConfiguration _config;

    public MembershipModel(ISiteService siteService, IMembershipPlanService membershipPlanService, IConfiguration config)
    {
        _siteService = siteService;
        _membershipPlanService = membershipPlanService;
        _config = config;
    }

    public string SiteName { get; set; } = "Contento";
    public string SiteTagline { get; set; } = "";
    public bool IsConfigured { get; set; }
    public IEnumerable<MembershipPlan> Plans { get; set; } = [];
    [BindProperty(SupportsGet = true)]
    public bool? Success { get; set; }
    [BindProperty(SupportsGet = true)]
    public bool? Canceled { get; set; }

    public async Task OnGetAsync()
    {
        var site = HttpContext.GetCurrentSite();
        SiteName = site.Name;
        SiteTagline = site.Tagline ?? "";

        var siteId = HttpContext.GetCurrentSiteId();
        Plans = await _membershipPlanService.GetAllAsync(siteId, activeOnly: true);

        var key = _config["Stripe:PublishableKey"];
        IsConfigured = Plans.Any() && !string.IsNullOrEmpty(key) && !key.StartsWith("${");
    }
}
