using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Audit;

public class IndexModel : PageModel
{
    private readonly IAuditLogService _auditLogService;
    private readonly ISiteService _siteService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IAuditLogService auditLogService, ISiteService siteService, ILogger<IndexModel> logger)
    {
        _auditLogService = auditLogService;
        _siteService = siteService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? DateTo { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ActionFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    public IEnumerable<AuditLog> AuditLogs { get; set; } = [];
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public async Task OnGetAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var pageSize = 30;

        DateTime? from = null;
        DateTime? to = null;

        if (DateTime.TryParse(DateFrom, out var parsedFrom))
            from = parsedFrom;
        if (DateTime.TryParse(DateTo, out var parsedTo))
            to = parsedTo.Date.AddDays(1).AddTicks(-1); // End of day

        try
        {
            AuditLogs = await _auditLogService.QueryAsync(
                siteId: siteId,
                action: ActionFilter,
                from: from,
                to: to,
                page: Page,
                pageSize: pageSize
            );
            TotalCount = await _auditLogService.GetTotalCountAsync(
                siteId: siteId,
                action: ActionFilter,
                from: from,
                to: to
            );
            TotalPages = (int)Math.Ceiling(TotalCount / (double)pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load audit logs in {Page}", nameof(IndexModel));
        }
    }

    public static string FormatAction(string action)
    {
        // Convert "post.publish" to "Post Publish"
        return string.Join(' ', action.Split('.')
            .Select(s => s.Length > 0 ? char.ToUpper(s[0]) + s[1..] : s));
    }
}
