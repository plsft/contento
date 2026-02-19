using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Controllers;

/// <summary>
/// REST API for 301/302 redirect management
/// </summary>
[Tags("Redirects")]
[ApiController]
[Route("api/v1/redirects")]
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
public class RedirectsApiController : ControllerBase
{
    private readonly IRedirectService _redirectService;
    private readonly ISiteService _siteService;

    public RedirectsApiController(IRedirectService redirectService, ISiteService siteService)
    {
        _redirectService = redirectService;
        _siteService = siteService;
    }

    [HttpGet]
    [EndpointSummary("List all redirects")]
    [EndpointDescription("Returns a paginated list of redirects for the current site with total count metadata.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var redirects = await _redirectService.GetAllAsync(siteId, page, pageSize);
        var total = await _redirectService.GetTotalCountAsync(siteId);

        return Ok(new
        {
            data = redirects,
            meta = new { page, pageSize, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    [HttpGet("{id}")]
    [EndpointSummary("Get a redirect by ID")]
    [EndpointDescription("Returns the full details of a specific redirect including hit count and last-hit timestamp.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get(string id)
    {
        if (!Guid.TryParse(id, out var redirectId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid redirect ID." } });

        var redirect = await _redirectService.GetByIdAsync(redirectId);
        if (redirect == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Redirect not found." } });

        return Ok(new { data = redirect });
    }

    [HttpPost]
    [EndpointSummary("Create a redirect")]
    [EndpointDescription("Creates a new 301 or 302 redirect for the current site.")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateRedirectRequest request)
    {
        try
        {
            var siteId = HttpContext.GetCurrentSiteId();

            var redirect = new Redirect
            {
                SiteId = siteId,
                FromPath = request.FromPath ?? "",
                ToPath = request.ToPath ?? "",
                StatusCode = request.StatusCode is 301 or 302 ? request.StatusCode : 301,
                Notes = request.Notes,
                IsActive = request.IsActive
            };

            var created = await _redirectService.CreateAsync(redirect);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, new { data = created });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpPut("{id}")]
    [EndpointSummary("Update a redirect")]
    [EndpointDescription("Updates an existing redirect's paths, status code, notes, or active state. Only provided fields are updated.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateRedirectRequest request)
    {
        if (!Guid.TryParse(id, out var redirectId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid redirect ID." } });

        try
        {
            var existing = await _redirectService.GetByIdAsync(redirectId);
            if (existing == null)
                return NotFound(new { error = new { code = "NOT_FOUND", message = "Redirect not found." } });

            if (request.FromPath != null) existing.FromPath = request.FromPath;
            if (request.ToPath != null) existing.ToPath = request.ToPath;
            if (request.StatusCode.HasValue) existing.StatusCode = request.StatusCode.Value;
            if (request.Notes != null) existing.Notes = request.Notes;
            if (request.IsActive.HasValue) existing.IsActive = request.IsActive.Value;

            var updated = await _redirectService.UpdateAsync(existing);
            return Ok(new { data = updated });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpDelete("{id}")]
    [EndpointSummary("Delete a redirect")]
    [EndpointDescription("Permanently deletes a redirect by its ID.")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
        if (!Guid.TryParse(id, out var redirectId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid redirect ID." } });

        var existing = await _redirectService.GetByIdAsync(redirectId);
        if (existing == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Redirect not found." } });

        await _redirectService.DeleteAsync(redirectId);
        return NoContent();
    }
}

public class CreateRedirectRequest
{
    public string? FromPath { get; set; }
    public string? ToPath { get; set; }
    public int StatusCode { get; set; } = 301;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateRedirectRequest
{
    public string? FromPath { get; set; }
    public string? ToPath { get; set; }
    public int? StatusCode { get; set; }
    public string? Notes { get; set; }
    public bool? IsActive { get; set; }
}
