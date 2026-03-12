using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages;

[AllowAnonymous]
public class CategoryModel : PageModel
{
    private const int PageSize = 10;

    private readonly IPostService _postService;
    private readonly ISiteService _siteService;
    private readonly ICategoryService _categoryService;

    public CategoryModel(IPostService postService, ISiteService siteService, ICategoryService categoryService)
    {
        _postService = postService;
        _siteService = siteService;
        _categoryService = categoryService;
    }

    public Category? CategoryItem { get; set; }
    public string SiteName { get; set; } = "Contento";
    public string SiteTagline { get; set; } = "";
    public IEnumerable<Post> Posts { get; set; } = [];
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug, [FromQuery] int page = 1)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var site = HttpContext.GetCurrentSite();
        var siteId = site.Id;

        SiteName = site.Name;
        SiteTagline = site.Tagline ?? "";

        CategoryItem = await _categoryService.GetBySlugAsync(siteId, slug);

        if (CategoryItem == null)
            return NotFound();

        TotalCount = await _postService.GetTotalCountAsync(siteId, status: "published", categoryId: CategoryItem.Id);
        TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
        CurrentPage = Math.Clamp(page, 1, Math.Max(1, TotalPages));

        Posts = await _postService.GetAllAsync(siteId, status: "published", categoryId: CategoryItem.Id, page: CurrentPage, pageSize: PageSize);

        return Page();
    }
}
