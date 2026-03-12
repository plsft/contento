using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages;

[AllowAnonymous]
public class SearchModel : PageModel
{
    private const int PageSize = 10;

    private readonly ISearchService _searchService;
    private readonly ISiteService _siteService;

    public SearchModel(ISearchService searchService, ISiteService siteService)
    {
        _searchService = searchService;
        _siteService = siteService;
    }

    public string SiteName { get; set; } = "Contento";
    public string SiteTagline { get; set; } = "";
    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }
    [BindProperty(SupportsGet = true, Name = "page")]
    public int CurrentPageInput { get; set; } = 1;
    public IEnumerable<Post> Results { get; set; } = [];
    public int TotalResults { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }

    public async Task OnGetAsync()
    {
        var site = HttpContext.GetCurrentSite();
        var siteId = site.Id;

        SiteName = site.Name;
        SiteTagline = site.Tagline ?? "";

        if (!string.IsNullOrWhiteSpace(Q))
        {

            TotalCount = await _searchService.GetSearchResultCountAsync(siteId, Q);
            TotalResults = TotalCount;
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            CurrentPage = Math.Clamp(CurrentPageInput, 1, Math.Max(1, TotalPages));

            Results = await _searchService.SearchPostsAsync(siteId, Q, CurrentPage, PageSize);
        }
    }
}
