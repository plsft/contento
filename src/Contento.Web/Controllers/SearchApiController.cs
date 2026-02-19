using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Controllers;

/// <summary>
/// REST API for full-text search across posts with typeahead suggestions
/// </summary>
[Tags("Search")]
[ApiController]
[Route("api/v1/search")]
public class SearchApiController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ISiteService _siteService;

    public SearchApiController(ISearchService searchService, ISiteService siteService)
    {
        _searchService = searchService;
        _siteService = siteService;
    }

    [HttpGet]
    [EndpointSummary("Search posts")]
    [EndpointDescription("Performs a full-text search across published posts with pagination and total result count metadata.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Search(
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = new { code = "MISSING_QUERY", message = "Search query parameter 'q' is required." } });

        var siteId = HttpContext.GetCurrentSiteId();
        var results = await _searchService.SearchPostsAsync(siteId, q, page, pageSize);
        var total = await _searchService.GetSearchResultCountAsync(siteId, q);

        return Ok(new
        {
            data = results,
            meta = new { page, pageSize, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    [HttpGet("suggestions")]
    [EndpointSummary("Get search suggestions")]
    [EndpointDescription("Returns typeahead search suggestions based on a partial query string, limited to the specified number of results.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Suggestions(
        [FromQuery] string? q = null,
        [FromQuery] int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = new { code = "MISSING_QUERY", message = "Search query parameter 'q' is required." } });

        var siteId = HttpContext.GetCurrentSiteId();
        var suggestions = await _searchService.GetSuggestionsAsync(siteId, q, limit);

        return Ok(new { data = suggestions });
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("app_user_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
