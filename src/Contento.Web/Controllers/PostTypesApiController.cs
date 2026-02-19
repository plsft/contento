using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Controllers;

[Tags("Post Types")]
[ApiController]
[Route("api/v1/post-types")]
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
public class PostTypesApiController : ControllerBase
{
    private readonly IPostTypeService _postTypeService;
    private readonly ISiteService _siteService;

    public PostTypesApiController(IPostTypeService postTypeService, ISiteService siteService)
    {
        _postTypeService = postTypeService;
        _siteService = siteService;
    }

    [HttpGet]
    [EndpointSummary("List all post types")]
    [EndpointDescription("Returns all post types defined for the current site, including their custom field schemas and display settings.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> List()
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var postTypes = await _postTypeService.GetAllAsync(siteId);
        return Ok(new { data = postTypes });
    }

    [HttpGet("{id}")]
    [EndpointSummary("Get a post type by ID")]
    [EndpointDescription("Returns the full details of a specific post type including its custom field definitions and settings.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(string id)
    {
        if (!Guid.TryParse(id, out var parsedId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid post type ID." } });

        var postType = await _postTypeService.GetByIdAsync(parsedId);
        if (postType == null)
            return NotFound();

        return Ok(new { data = postType });
    }

    [HttpPost]
    [EndpointSummary("Create a post type")]
    [EndpointDescription("Creates a new custom post type with the specified name, slug, icon, field schema, and settings.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreatePostTypeRequest request)
    {
        try
        {
            var siteId = HttpContext.GetCurrentSiteId();

            var postType = new PostType
            {
                SiteId = siteId,
                Name = request.Name ?? string.Empty,
                Slug = request.Slug ?? string.Empty,
                Icon = request.Icon,
                Fields = request.Fields ?? "[]",
                Settings = request.Settings ?? "{}"
            };

            var created = await _postTypeService.CreateAsync(postType);
            return Ok(new { data = created });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpPut("{id}")]
    [EndpointSummary("Update a post type")]
    [EndpointDescription("Updates an existing post type's name, slug, icon, field schema, or settings. Only provided fields are modified.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePostTypeRequest request)
    {
        if (!Guid.TryParse(id, out var parsedId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid post type ID." } });

        var existing = await _postTypeService.GetByIdAsync(parsedId);
        if (existing == null)
            return NotFound();

        try
        {
            existing.Name = request.Name ?? existing.Name;
            existing.Slug = request.Slug ?? existing.Slug;
            existing.Icon = request.Icon ?? existing.Icon;
            existing.Fields = request.Fields ?? existing.Fields;
            existing.Settings = request.Settings ?? existing.Settings;

            var updated = await _postTypeService.UpdateAsync(existing);
            return Ok(new { data = updated });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpDelete("{id}")]
    [EndpointSummary("Delete a post type")]
    [EndpointDescription("Permanently deletes a custom post type. Built-in system post types cannot be deleted.")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
        if (!Guid.TryParse(id, out var parsedId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid post type ID." } });

        try
        {
            await _postTypeService.DeleteAsync(parsedId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = new { code = "SYSTEM_TYPE", message = ex.Message } });
        }
    }
}

public class CreatePostTypeRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Icon { get; set; }
    public string? Fields { get; set; }
    public string? Settings { get; set; }
}

public class UpdatePostTypeRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Icon { get; set; }
    public string? Fields { get; set; }
    public string? Settings { get; set; }
}
