using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Controllers;

/// <summary>
/// REST API for layout template management and component configuration
/// </summary>
[Tags("Layouts")]
[ApiController]
[Route("api/v1/layouts")]
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
public class LayoutsApiController : ControllerBase
{
    private readonly ILayoutService _layoutService;
    private readonly ISiteService _siteService;

    public LayoutsApiController(ILayoutService layoutService, ISiteService siteService)
    {
        _layoutService = layoutService;
        _siteService = siteService;
    }

    [HttpGet]
    [EndpointSummary("List all layouts")]
    [EndpointDescription("Returns a paginated list of layout templates for the current site, including their structure and component configuration.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var layouts = await _layoutService.GetAllBySiteAsync(siteId, page, pageSize);
        var layoutList = layouts.ToList();

        return Ok(new
        {
            data = layoutList,
            meta = new { page, pageSize, totalCount = layoutList.Count, totalPages = 1 }
        });
    }

    [HttpGet("{id}")]
    [EndpointSummary("Get a layout by ID")]
    [EndpointDescription("Returns a specific layout with its full structure and associated component configuration.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get(string id)
    {
        if (!Guid.TryParse(id, out var layoutId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid layout ID." } });

        var result = await _layoutService.GetWithComponentsAsync(layoutId);
        if (result == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Layout not found." } });

        return Ok(new { data = new { layout = result.Value.Layout, components = result.Value.Components } });
    }

    [HttpPost]
    [EndpointSummary("Create a layout")]
    [EndpointDescription("Creates a new layout template for the current site with the specified structure, CSS, and JavaScript configuration.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateLayoutRequest request)
    {
        try
        {
            var siteId = HttpContext.GetCurrentSiteId();

            var layout = new Layout
            {
                SiteId = siteId,
                Name = request.Name ?? "Untitled",
                Slug = request.Slug ?? "",
                Description = request.Description,
                IsDefault = request.IsDefault,
                Structure = request.Structure ?? "{}",
                HeadContent = request.HeadContent,
                CustomCss = request.CustomCss,
                CustomJs = request.CustomJs
            };

            var created = await _layoutService.CreateAsync(layout);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, new { data = created });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpPut("{id}")]
    [EndpointSummary("Update a layout")]
    [EndpointDescription("Updates an existing layout's name, structure, custom CSS, JavaScript, or default status. Only provided fields are modified.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateLayoutRequest request)
    {
        if (!Guid.TryParse(id, out var layoutId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid layout ID." } });

        try
        {
            var existing = await _layoutService.GetByIdAsync(layoutId);
            if (existing == null)
                return NotFound(new { error = new { code = "NOT_FOUND", message = "Layout not found." } });

            if (request.Name != null) existing.Name = request.Name;
            if (request.Slug != null) existing.Slug = request.Slug;
            if (request.Description != null) existing.Description = request.Description;
            if (request.IsDefault.HasValue) existing.IsDefault = request.IsDefault.Value;
            if (request.Structure != null) existing.Structure = request.Structure;
            if (request.HeadContent != null) existing.HeadContent = request.HeadContent;
            if (request.CustomCss != null) existing.CustomCss = request.CustomCss;
            if (request.CustomJs != null) existing.CustomJs = request.CustomJs;

            var updated = await _layoutService.UpdateAsync(existing);
            return Ok(new { data = updated });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpDelete("{id}")]
    [EndpointSummary("Delete a layout")]
    [EndpointDescription("Permanently deletes a layout by its ID. Default layouts cannot be deleted while other layouts exist.")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
        if (!Guid.TryParse(id, out var layoutId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid layout ID." } });

        var existing = await _layoutService.GetByIdAsync(layoutId);
        if (existing == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Layout not found." } });

        try
        {
            await _layoutService.DeleteAsync(layoutId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpPost("{id}/set-default")]
    [EndpointSummary("Set layout as default")]
    [EndpointDescription("Designates the specified layout as the default template for new posts. Removes default status from any other layout.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SetDefault(string id)
    {
        if (!Guid.TryParse(id, out var layoutId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid layout ID." } });

        var layout = await _layoutService.GetByIdAsync(layoutId);
        if (layout == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Layout not found." } });

        await _layoutService.SetDefaultAsync(layoutId);
        var updated = await _layoutService.GetByIdAsync(layoutId);
        return Ok(new { data = updated });
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("app_user_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

public class CreateLayoutRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public string? Structure { get; set; }
    public string? HeadContent { get; set; }
    public string? CustomCss { get; set; }
    public string? CustomJs { get; set; }
}

public class UpdateLayoutRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public bool? IsDefault { get; set; }
    public string? Structure { get; set; }
    public string? HeadContent { get; set; }
    public string? CustomCss { get; set; }
    public string? CustomJs { get; set; }
}
