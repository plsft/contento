using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages;

[AllowAnonymous]
public class PostModel : PageModel
{
    private readonly IPostService _postService;
    private readonly ISiteService _siteService;
    private readonly ICommentService _commentService;
    private readonly ITrafficService _trafficService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<PostModel> _logger;

    public PostModel(
        IPostService postService,
        ISiteService siteService,
        ICommentService commentService,
        ITrafficService trafficService,
        ISubscriptionService subscriptionService,
        ILogger<PostModel> logger)
    {
        _postService = postService;
        _siteService = siteService;
        _commentService = commentService;
        _trafficService = trafficService;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public Post PostItem { get; set; } = default!;
    public string SiteName { get; set; } = "Contento";
    public string SiteTagline { get; set; } = "";
    public IEnumerable<Comment> Comments { get; set; } = [];
    public bool IsMemberContent { get; set; }
    public bool ShowPaywall { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var site = HttpContext.GetCurrentSite();
        var siteId = site.Id;

        SiteName = site.Name;
        SiteTagline = site.Tagline ?? "";
        var post = await _postService.GetBySlugAsync(siteId, slug);

        if (post == null || post.Status != "published")
            return NotFound();

        PostItem = post;

        // Membership gate
        if (post.Visibility == "members" || post.Visibility == "premium")
        {
            IsMemberContent = true;
            var memberEmail = Request.Cookies["contento-member-email"]
                              ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(memberEmail))
            {
                ShowPaywall = true;
            }
            else
            {
                var hasAccess = await _subscriptionService.HasActiveMembershipAsync(memberEmail, siteId);
                if (!hasAccess)
                    ShowPaywall = true;
            }
        }

        Comments = await _commentService.GetByPostAsync(post.Id);

        // Set ViewData for JSON-LD structured data
        ViewData["PostId"] = post.Id;
        ViewData["DatePublished"] = post.PublishedAt?.ToString("o");
        ViewData["DateModified"] = post.UpdatedAt.ToString("o");
        ViewData["AuthorName"] = post.AuthorId.ToString();

        // Track page view
        try
        {
            var ipHash = Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(
                        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown")));

            await _trafficService.RecordPageViewAsync(new PageView
            {
                PostId = post.Id,
                SessionId = HttpContext.Session.Id,
                IpHash = ipHash,
                Referrer = Request.Headers.Referer.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                DeviceType = DetectDeviceType(Request.Headers.UserAgent.ToString())
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record page view in {Page}", nameof(PostModel));
        }

        return Page();
    }

    private static string DetectDeviceType(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "unknown";
        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("mobile") || ua.Contains("android") || ua.Contains("iphone"))
            return "mobile";
        if (ua.Contains("tablet") || ua.Contains("ipad"))
            return "tablet";
        return "desktop";
    }
}
