using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Posts;

public class IndexModel : PageModel
{
    private readonly IPostService _postService;
    private readonly ISiteService _siteService;
    private readonly IPostTypeService _postTypeService;

    public IndexModel(IPostService postService, ISiteService siteService, IPostTypeService postTypeService)
    {
        _postService = postService;
        _siteService = siteService;
        _postTypeService = postTypeService;
    }

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "all";

    [BindProperty(SupportsGet = true)]
    public string? SearchQuery { get; set; }

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string? PostTypeSlug { get; set; }

    public IEnumerable<Post> Posts { get; set; } = [];
    public IEnumerable<PostType> PostTypes { get; set; } = [];
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public async Task OnGetAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var status = Status == "all" ? null : Status;
        var pageSize = 20;

        PostTypes = await _postTypeService.GetAllAsync(siteId);
        Posts = await _postService.GetAllAsync(siteId, status: status, search: SearchQuery, page: Page, pageSize: pageSize);
        TotalCount = await _postService.GetTotalCountAsync(siteId, status);
        TotalPages = (int)Math.Ceiling(TotalCount / (double)pageSize);
    }
}
