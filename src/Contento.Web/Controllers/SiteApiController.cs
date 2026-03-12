using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Web.Middleware;

namespace Contento.Web.Controllers;

[Tags("Site")]
[ApiController]
[Route("api/v1")]
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
public class SiteApiController : ControllerBase
{
    private readonly ISiteService _siteService;
    private readonly ICategoryService _categoryService;
    private readonly ISearchService _searchService;
    private readonly ILayoutService _layoutService;
    private readonly IThemeService _themeService;
    private readonly IPluginService _pluginService;

    public SiteApiController(
        ISiteService siteService,
        ICategoryService categoryService,
        ISearchService searchService,
        ILayoutService layoutService,
        IThemeService themeService,
        IPluginService pluginService)
    {
        _siteService = siteService;
        _categoryService = categoryService;
        _searchService = searchService;
        _layoutService = layoutService;
        _themeService = themeService;
        _pluginService = pluginService;
    }

    [HttpGet("site")]
    [EndpointSummary("Get current site")]
    [EndpointDescription("Returns the site associated with the current request context, including its name, settings, and configuration.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSite()
    {
        var site = HttpContext.TryGetCurrentSite();
        return site == null ? NotFound() : Ok(new { data = site });
    }

    [HttpGet("categories")]
    [EndpointSummary("List site categories")]
    [EndpointDescription("Returns all categories for the current site as a flat list.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ListCategories()
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var categories = await _categoryService.GetAllBySiteAsync(siteId);
        return Ok(new { data = categories });
    }

    [HttpGet("search")]
    [EndpointSummary("Search site content")]
    [EndpointDescription("Performs a full-text search across posts for the current site with pagination support.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = new { code = "MISSING_QUERY", message = "Search query required." } });

        var siteId = HttpContext.GetCurrentSiteId();
        var results = await _searchService.SearchPostsAsync(siteId, q, page, pageSize);
        return Ok(new { data = results });
    }

    [HttpGet("layouts")]
    [EndpointSummary("List site layouts")]
    [EndpointDescription("Returns all layout templates available for the current site.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ListLayouts()
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var layouts = await _layoutService.GetAllBySiteAsync(siteId);
        return Ok(new { data = layouts });
    }

    [HttpGet("themes")]
    [EndpointSummary("List site themes")]
    [EndpointDescription("Returns all available themes that can be applied to the site.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ListThemes()
    {
        var themes = await _themeService.GetAllAsync();
        return Ok(new { data = themes });
    }

    [HttpGet("plugins")]
    [EndpointSummary("List site plugins")]
    [EndpointDescription("Returns all installed plugins for the current site with their enabled/disabled status.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ListPlugins()
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var plugins = await _pluginService.GetAllBySiteAsync(siteId);
        return Ok(new { data = plugins });
    }
}
