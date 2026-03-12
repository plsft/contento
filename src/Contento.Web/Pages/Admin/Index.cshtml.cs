using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin;

public class IndexModel : PageModel
{
    private readonly IPostService _postService;
    private readonly ICommentService _commentService;
    private readonly ISiteService _siteService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IPostService postService, ICommentService commentService, ISiteService siteService, ILogger<IndexModel> logger)
    {
        _postService = postService;
        _commentService = commentService;
        _siteService = siteService;
        _logger = logger;
    }

    public string UserName { get; set; } = "Writer";
    public string TimeOfDay { get; set; } = "morning";
    public int PublishedCount { get; set; }
    public int DraftCount { get; set; }
    public int ViewsThisMonth { get; set; }
    public int PendingComments { get; set; }
    public IEnumerable<Post> RecentDrafts { get; set; } = [];
    public IEnumerable<Comment> RecentComments { get; set; } = [];

    public async Task OnGetAsync()
    {
        UserName = User.FindFirst("display_name")?.Value ?? "Writer";

        var hour = DateTime.Now.Hour;
        TimeOfDay = hour switch
        {
            < 12 => "morning",
            < 17 => "afternoon",
            _ => "evening"
        };

        var siteId = HttpContext.GetCurrentSiteId();

        try
        {
            PublishedCount = await _postService.GetTotalCountAsync(siteId, "published");
            DraftCount = await _postService.GetTotalCountAsync(siteId, "draft");
            RecentDrafts = await _postService.GetAllAsync(siteId, status: "draft", page: 1, pageSize: 5);
            PendingComments = await _commentService.GetCountBySiteAsync(siteId, "pending");
            RecentComments = await _commentService.GetModerationQueueAsync(siteId, "pending", page: 1, pageSize: 5);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load dashboard data in {Page}", nameof(IndexModel));
        }
    }
}
