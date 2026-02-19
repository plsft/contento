using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Web.Pages.Admin.Posts;

public class MetricsModel : PageModel
{
    private readonly IPostService _postService;
    private readonly ITrafficService _trafficService;
    private readonly ILogger<MetricsModel> _logger;

    public MetricsModel(IPostService postService, ITrafficService trafficService, ILogger<MetricsModel> logger)
    {
        _postService = postService;
        _trafficService = trafficService;
        _logger = logger;
    }

    public Post? CurrentPost { get; set; }

    public long TotalViews { get; set; }
    public long UniqueVisitors { get; set; }
    public int? AvgTimeOnPageSeconds { get; set; }
    public decimal? BounceRate { get; set; }

    public IEnumerable<(string Referrer, long Count)> TopReferrers { get; set; } = [];
    public IEnumerable<(string DeviceType, long Count)> DeviceBreakdown { get; set; } = [];
    public IEnumerable<TrafficDaily> DailyStats { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? DateTo { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (!Guid.TryParse(id, out var postId))
            return NotFound();

        try
        {
            CurrentPost = await _postService.GetByIdAsync(postId);
            if (CurrentPost == null)
                return NotFound();

            var from = DateTime.TryParse(DateFrom, out var parsedFrom)
                ? parsedFrom
                : DateTime.UtcNow.AddDays(-30);
            var to = DateTime.TryParse(DateTo, out var parsedTo)
                ? parsedTo
                : DateTime.UtcNow;

            var metrics = await _trafficService.GetPostMetricsAsync(postId, from, to);
            TotalViews = metrics.TotalViews;
            UniqueVisitors = metrics.UniqueVisitors;
            AvgTimeOnPageSeconds = metrics.AvgTimeOnPageSeconds;
            BounceRate = metrics.BounceRate;

            TopReferrers = await _trafficService.GetTopReferrersAsync(postId, from, to);
            DeviceBreakdown = await _trafficService.GetDeviceBreakdownAsync(postId, from, to);
            DailyStats = await _trafficService.GetDailyStatsAsync(postId, from, to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load post metrics in {Page}", nameof(MetricsModel));
        }

        return Page();
    }
}
