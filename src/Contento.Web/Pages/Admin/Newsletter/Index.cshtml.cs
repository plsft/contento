using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Newsletter;

public class IndexModel : PageModel
{
    private readonly INewsletterService _newsletterService;
    private readonly IPostService _postService;
    private readonly ISiteService _siteService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(INewsletterService newsletterService, IPostService postService, ISiteService siteService, ILogger<IndexModel> logger)
    {
        _newsletterService = newsletterService;
        _postService = postService;
        _siteService = siteService;
        _logger = logger;
    }

    public int SubscriberCount { get; set; }
    public IEnumerable<NewsletterCampaign> Campaigns { get; set; } = [];
    public IEnumerable<Post> PublishedPosts { get; set; } = [];

    public async Task OnGetAsync()
    {
        try
        {
            var siteId = HttpContext.GetCurrentSiteId();

            SubscriberCount = await _newsletterService.GetSubscriberCountAsync(siteId);
            Campaigns = await _newsletterService.GetCampaignsAsync(siteId, 1, 20);
            PublishedPosts = await _postService.GetAllAsync(siteId, status: "published", page: 1, pageSize: 50);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load newsletter data in {Page}", nameof(IndexModel));
        }
    }
}
