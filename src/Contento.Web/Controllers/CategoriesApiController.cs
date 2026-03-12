using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Controllers;

/// <summary>
/// REST API for hierarchical category management
/// </summary>
[Tags("Categories")]
[ApiController]
[Route("api/v1/categories")]
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
public class CategoriesApiController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ISiteService _siteService;

    public CategoriesApiController(ICategoryService categoryService, ISiteService siteService)
    {
        _categoryService = categoryService;
        _siteService = siteService;
    }

    [HttpGet]
    [EndpointSummary("List all categories")]
    [EndpointDescription("Returns a paginated flat list of categories for the current site with total count metadata.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var categories = await _categoryService.GetAllBySiteAsync(siteId, page, pageSize);
        var total = await _categoryService.GetTotalCountAsync(siteId);

        return Ok(new
        {
            data = categories,
            meta = new { page, pageSize, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    [HttpGet("tree")]
    [EndpointSummary("Get category tree")]
    [EndpointDescription("Returns categories organized as a hierarchical tree structure with nested children, suitable for navigation menus.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTree()
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var tree = await _categoryService.GetTreeAsync(siteId);

        return Ok(new { data = tree });
    }

    [HttpGet("{id}")]
    [EndpointSummary("Get a category by ID")]
    [EndpointDescription("Returns the full details of a specific category including its name, slug, description, and parent relationship.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get(string id)
    {
        if (!Guid.TryParse(id, out var categoryId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid category ID." } });

        var category = await _categoryService.GetByIdAsync(categoryId);
        if (category == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Category not found." } });

        return Ok(new { data = category });
    }

    [HttpPost]
    [EndpointSummary("Create a category")]
    [EndpointDescription("Creates a new category for the current site. Supports hierarchical nesting via the optional parentId field.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        try
        {
            var siteId = HttpContext.GetCurrentSiteId();

            var category = new Category
            {
                SiteId = siteId,
                Name = request.Name ?? "Untitled",
                Slug = request.Slug ?? "",
                Description = request.Description,
                ParentId = request.ParentId,
                SortOrder = request.SortOrder
            };

            var created = await _categoryService.CreateAsync(category);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, new { data = created });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpPut("{id}")]
    [EndpointSummary("Update a category")]
    [EndpointDescription("Updates an existing category's name, slug, description, parent, or sort order. Only provided fields are updated.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCategoryRequest request)
    {
        if (!Guid.TryParse(id, out var categoryId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid category ID." } });

        try
        {
            var existing = await _categoryService.GetByIdAsync(categoryId);
            if (existing == null)
                return NotFound(new { error = new { code = "NOT_FOUND", message = "Category not found." } });

            if (request.Name != null) existing.Name = request.Name;
            if (request.Slug != null) existing.Slug = request.Slug;
            if (request.Description != null) existing.Description = request.Description;
            if (request.ParentId.HasValue) existing.ParentId = request.ParentId.Value;
            if (request.SortOrder.HasValue) existing.SortOrder = request.SortOrder.Value;

            var updated = await _categoryService.UpdateAsync(existing);
            return Ok(new { data = updated });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpDelete("{id}")]
    [EndpointSummary("Delete a category")]
    [EndpointDescription("Permanently deletes a category by its ID. Posts assigned to this category will be unlinked.")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
        if (!Guid.TryParse(id, out var categoryId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid category ID." } });

        var existing = await _categoryService.GetByIdAsync(categoryId);
        if (existing == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Category not found." } });

        await _categoryService.DeleteAsync(categoryId);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("app_user_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

public class CreateCategoryRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateCategoryRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public int? SortOrder { get; set; }
}
