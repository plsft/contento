using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Pseo.Analytics;

public class IndexModel : PageModel
{
    private readonly IPseoProjectService _projectService;
    private readonly IPseoAnalyticsService _analyticsService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IPseoProjectService projectService,
        IPseoAnalyticsService analyticsService,
        ILogger<IndexModel> logger)
    {
        _projectService = projectService;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? ProjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Days { get; set; } = 30;

    public List<PseoProject> Projects { get; set; } = [];
    public PseoProject? SelectedProject { get; set; }
    public PseoAnalyticsSummary? Summary { get; set; }
    public List<PseoPageAnalytics> TopPages { get; set; } = [];
    public List<NichePerformance> NicheStats { get; set; } = [];
    public List<PseoPage> ZeroTrafficPages { get; set; } = [];
    public bool IsGscConnected { get; set; }
    public string? GscAuthUrl { get; set; }
    public string? LastSyncTime { get; set; }

    public async Task OnGetAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();

        try
        {
            Projects = await _projectService.GetBySiteIdAsync(siteId);

            if (ProjectId == null && Projects.Count > 0)
                ProjectId = Projects[0].Id;

            if (ProjectId == null)
                return;

            SelectedProject = await _projectService.GetByIdAsync(ProjectId.Value);
            if (SelectedProject == null)
                return;

            // Check GSC connection status
            var settings = System.Text.Json.Nodes.JsonNode.Parse(SelectedProject.Settings ?? "{}") as System.Text.Json.Nodes.JsonObject;
            var accessToken = settings?["gsc_access_token"]?.GetValue<string>();
            IsGscConnected = !string.IsNullOrEmpty(accessToken);
            LastSyncTime = settings?["gsc_last_sync"]?.GetValue<string>();

            if (!IsGscConnected)
            {
                try
                {
                    var redirectUri = $"{Request.Scheme}://{Request.Host}/admin/pseo/analytics?handler=GscCallback";
                    GscAuthUrl = await _analyticsService.GetGscAuthUrlAsync(ProjectId.Value, redirectUri);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not generate GSC auth URL — client ID may not be configured");
                }
            }

            // Load analytics data
            Summary = await _analyticsService.GetProjectSummaryAsync(ProjectId.Value, Days);
            TopPages = await _analyticsService.GetTopPagesAsync(ProjectId.Value, Days);
            NicheStats = await _analyticsService.GetNichePerformanceAsync(ProjectId.Value, Days);
            ZeroTrafficPages = await _analyticsService.GetZeroTrafficPagesAsync(ProjectId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load pSEO analytics dashboard");
        }
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (ProjectId == null)
            return BadRequest();

        var csv = await _analyticsService.ExportCsvAsync(ProjectId.Value, Days);
        return File(csv, "text/csv", $"pseo-analytics-{DateTime.UtcNow:yyyy-MM-dd}.csv");
    }

    public async Task<IActionResult> OnPostSyncAsync()
    {
        if (ProjectId == null)
            return BadRequest();

        try
        {
            await _analyticsService.SyncGscDataAsync(ProjectId.Value);
            TempData["Toast"] = "GSC data synced successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual GSC sync failed for project {ProjectId}", ProjectId.Value);
            TempData["ToastError"] = $"Sync failed: {ex.Message}";
        }

        return RedirectToPage(new { projectId = ProjectId, days = Days });
    }

    public async Task<IActionResult> OnGetGscCallbackAsync(string code, string state)
    {
        if (string.IsNullOrEmpty(code))
            return RedirectToPage();

        try
        {
            // Decode project ID from state
            var projectIdStr = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(state));
            if (!Guid.TryParse(projectIdStr, out var projectId))
                return RedirectToPage();

            var redirectUri = $"{Request.Scheme}://{Request.Host}/admin/pseo/analytics?handler=GscCallback";
            await _analyticsService.ExchangeGscCodeAsync(projectId, code, redirectUri);
            TempData["Toast"] = "Google Search Console connected successfully.";
            return RedirectToPage(new { projectId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GSC callback failed");
            TempData["ToastError"] = $"GSC connection failed: {ex.Message}";
            return RedirectToPage();
        }
    }
}
