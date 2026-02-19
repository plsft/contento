using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Redirects;

public class IndexModel : PageModel
{
    private readonly IRedirectService _redirectService;
    private readonly ISiteService _siteService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IRedirectService redirectService, ISiteService siteService, ILogger<IndexModel> logger)
    {
        _redirectService = redirectService;
        _siteService = siteService;
        _logger = logger;
    }

    public IEnumerable<Redirect> Redirects { get; set; } = [];
    public int TotalCount { get; set; }

    [BindProperty]
    public string FromPath { get; set; } = string.Empty;

    [BindProperty]
    public string ToPath { get; set; } = string.Empty;

    [BindProperty]
    public new int StatusCode { get; set; } = 301;

    [BindProperty]
    public string? Notes { get; set; }

    [BindProperty]
    public bool IsActive { get; set; } = true;

    [BindProperty]
    public Guid? EditId { get; set; }

    public async Task OnGetAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();

        try
        {
            Redirects = await _redirectService.GetAllAsync(siteId);
            TotalCount = await _redirectService.GetTotalCountAsync(siteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load redirects in {Page}", nameof(IndexModel));
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();

        try
        {
            var redirect = new Redirect
            {
                SiteId = siteId,
                FromPath = NormalizePath(FromPath),
                ToPath = ToPath,
                StatusCode = StatusCode is 301 or 302 ? StatusCode : 301,
                Notes = Notes,
                IsActive = IsActive
            };
            await _redirectService.CreateAsync(redirect);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create redirect in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        if (EditId == null) return RedirectToPage();

        try
        {
            var redirect = await _redirectService.GetByIdAsync(EditId.Value);
            if (redirect != null)
            {
                redirect.FromPath = NormalizePath(FromPath);
                redirect.ToPath = ToPath;
                redirect.StatusCode = StatusCode is 301 or 302 ? StatusCode : 301;
                redirect.Notes = Notes;
                redirect.IsActive = IsActive;
                await _redirectService.UpdateAsync(redirect);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update redirect in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _redirectService.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete redirect in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;
        path = path.Trim().ToLowerInvariant();
        if (!path.StartsWith('/')) path = "/" + path;
        return path;
    }
}
