using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.PostTypes;

public class IndexModel : PageModel
{
    private readonly IPostTypeService _postTypeService;
    private readonly ISiteService _siteService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IPostTypeService postTypeService, ISiteService siteService, ILogger<IndexModel> logger)
    {
        _postTypeService = postTypeService;
        _siteService = siteService;
        _logger = logger;
    }

    public IEnumerable<PostType> PostTypes { get; set; } = [];

    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string Slug { get; set; } = "";
    [BindProperty] public string? Icon { get; set; }

    public async Task OnGetAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();
        PostTypes = await _postTypeService.GetAllAsync(siteId);
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            var siteId = HttpContext.GetCurrentSiteId();
            var postType = new PostType
            {
                SiteId = siteId,
                Name = Name,
                Slug = Slug,
                Icon = Icon
            };
            await _postTypeService.CreateAsync(postType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create post type");
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _postTypeService.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete post type");
        }
        return RedirectToPage();
    }
}
