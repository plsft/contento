using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Membership;

public class IndexModel : PageModel
{
    private readonly INewsletterService _newsletterService;
    private readonly IMembershipPlanService _membershipPlanService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(INewsletterService newsletterService, IMembershipPlanService membershipPlanService, ILogger<IndexModel> logger)
    {
        _newsletterService = newsletterService;
        _membershipPlanService = membershipPlanService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "dashboard";

    public int TotalMembers { get; set; }
    public int PremiumMembers { get; set; }
    public int ActivePlans { get; set; }
    public IEnumerable<Subscriber> Members { get; set; } = [];
    public IEnumerable<MembershipPlan> Plans { get; set; } = [];

    // Plan form
    [BindProperty]
    public string PlanName { get; set; } = "";
    [BindProperty]
    public string PlanSlug { get; set; } = "";
    [BindProperty]
    public string? PlanDescription { get; set; }
    [BindProperty]
    public decimal PlanPrice { get; set; }
    [BindProperty]
    public string PlanInterval { get; set; } = "monthly";
    [BindProperty]
    public string? PlanStripePriceId { get; set; }
    [BindProperty]
    public int PlanTrialDays { get; set; }
    [BindProperty]
    public int PlanAccessLevel { get; set; } = 1;
    [BindProperty]
    public string? PlanFeatures { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostCreatePlanAsync()
    {
        try
        {
            var siteId = HttpContext.GetCurrentSiteId();
            var features = string.IsNullOrWhiteSpace(PlanFeatures)
                ? "[]"
                : System.Text.Json.JsonSerializer.Serialize(
                    PlanFeatures.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

            var plan = new MembershipPlan
            {
                Id = Guid.NewGuid(),
                SiteId = siteId,
                Name = PlanName,
                Slug = PlanSlug,
                Description = PlanDescription,
                Price = PlanPrice,
                BillingInterval = PlanInterval,
                StripePriceId = PlanStripePriceId,
                TrialDays = PlanTrialDays,
                AccessLevel = PlanAccessLevel,
                Features = features,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _membershipPlanService.CreateAsync(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create membership plan");
        }

        Tab = "plans";
        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeletePlanAsync(Guid id)
    {
        try
        {
            await _membershipPlanService.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete membership plan");
        }

        Tab = "plans";
        await LoadDataAsync();
        return Page();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var siteId = HttpContext.GetCurrentSiteId();

            var allSubscribers = await _newsletterService.GetActiveSubscribersAsync(siteId);
            var subscriberList = allSubscribers.ToList();
            TotalMembers = subscriberList.Count;
            PremiumMembers = subscriberList.Count(s => s.MembershipTier != "free");
            Members = subscriberList.OrderByDescending(s => s.CreatedAt);

            Plans = await _membershipPlanService.GetAllAsync(siteId, activeOnly: false);
            ActivePlans = Plans.Count(p => p.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load membership data in {Page}", nameof(IndexModel));
        }
    }
}
