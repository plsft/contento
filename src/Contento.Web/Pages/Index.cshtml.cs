using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private const int PageSize = 10;

    private readonly IPostService _postService;
    private readonly ISiteService _siteService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IPostService postService, ISiteService siteService, ILogger<IndexModel> logger)
    {
        _postService = postService;
        _siteService = siteService;
        _logger = logger;
    }

    public string SiteName { get; set; } = "Contento";
    public string SiteTagline { get; set; } = "";
    public IEnumerable<Post> Posts { get; set; } = [];
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }

    public async Task OnGetAsync([FromQuery] int page = 1)
    {
        try
        {
            var site = HttpContext.GetCurrentSite();
            var siteId = site.Id;

            SiteName = site.Name;
            SiteTagline = site.Tagline ?? "";

            TotalCount = await _postService.GetTotalCountAsync(siteId, status: "published");
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            CurrentPage = Math.Clamp(page, 1, Math.Max(1, TotalPages));

            Posts = await _postService.GetAllAsync(siteId, status: "published", page: CurrentPage, pageSize: PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load homepage data in {Page}", nameof(IndexModel));
        }
    }
}
