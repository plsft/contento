using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Comments;

public class IndexModel : PageModel
{
    private readonly ICommentService _commentService;
    private readonly ISiteService _siteService;
    private readonly ISpamService _spamService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ICommentService commentService, ISiteService siteService, ISpamService spamService, ILogger<IndexModel> logger)
    {
        _commentService = commentService;
        _siteService = siteService;
        _spamService = spamService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "pending";

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    public IEnumerable<Comment> Comments { get; set; } = [];
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int SpamCount { get; set; }
    public int TrashCount { get; set; }
    public SpamStats? SpamStats { get; set; }

    public async Task OnGetAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var pageSize = 20;

        try
        {
            Comments = await _commentService.GetModerationQueueAsync(siteId, status: Status, page: Page, pageSize: pageSize);
            TotalCount = await _commentService.GetCountBySiteAsync(siteId, Status);
            TotalPages = (int)Math.Ceiling(TotalCount / (double)pageSize);

            PendingCount = await _commentService.GetCountBySiteAsync(siteId, "pending");
            ApprovedCount = await _commentService.GetCountBySiteAsync(siteId, "approved");
            SpamCount = await _commentService.GetCountBySiteAsync(siteId, "spam");
            TrashCount = await _commentService.GetCountBySiteAsync(siteId, "trash");
            SpamStats = await _spamService.GetStatsAsync(siteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load comments in {Page}", nameof(IndexModel));
        }
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid id)
    {
        try
        {
            await _commentService.ApproveAsync(id);
            await _spamService.TrainHamAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve comment in {Page}", nameof(IndexModel));
        }
        return RedirectToPage(new { status = Status, page = Page });
    }

    public async Task<IActionResult> OnPostSpamAsync(Guid id)
    {
        try
        {
            await _commentService.MarkSpamAsync(id);
            await _spamService.TrainSpamAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark comment as spam in {Page}", nameof(IndexModel));
        }
        return RedirectToPage(new { status = Status, page = Page });
    }

    public async Task<IActionResult> OnPostTrashAsync(Guid id)
    {
        try
        {
            await _commentService.TrashAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trash comment in {Page}", nameof(IndexModel));
        }
        return RedirectToPage(new { status = Status, page = Page });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _commentService.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete comment in {Page}", nameof(IndexModel));
        }
        return RedirectToPage(new { status = Status, page = Page });
    }
}
