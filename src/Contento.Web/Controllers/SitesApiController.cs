using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Web.Controllers;

[Tags("Sites")]
[ApiController]
[Route("api/v1/sites")]
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
public class SitesApiController : ControllerBase
{
    private readonly ISiteService _siteService;

    public SitesApiController(ISiteService siteService)
    {
        _siteService = siteService;
    }

    [HttpGet]
    [EndpointSummary("List all sites")]
    [EndpointDescription("Returns a paginated list of all sites in the multi-site installation.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var sites = await _siteService.GetAllAsync(page, pageSize);
        return Ok(new { data = sites });
    }

    [HttpGet("{id}")]
    [EndpointSummary("Get a site by ID")]
    [EndpointDescription("Returns the full details of a specific site including its name, slug, domain, and configuration.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(string id)
    {
        if (!Guid.TryParse(id, out var parsedId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid site ID." } });

        var site = await _siteService.GetByIdAsync(parsedId);
        if (site == null)
            return NotFound();

        return Ok(new { data = site });
    }

    [HttpPost]
    [EndpointSummary("Create a new site")]
    [EndpointDescription("Creates a new site in the multi-site installation with the specified name, slug, and optional custom domain.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateSiteRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            var site = new Site
            {
                Name = request.Name ?? string.Empty,
                Slug = request.Slug ?? string.Empty,
                Domain = request.Domain,
                CreatedBy = userId != Guid.Empty ? userId : null
            };

            var created = await _siteService.CreateAsync(site);
            return Ok(new { data = created });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpPut("{id}")]
    [EndpointSummary("Update a site")]
    [EndpointDescription("Updates an existing site's name, slug, or domain. Only provided fields are modified.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateSiteRequest request)
    {
        if (!Guid.TryParse(id, out var parsedId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid site ID." } });

        var existing = await _siteService.GetByIdAsync(parsedId);
        if (existing == null)
            return NotFound();

        try
        {
            existing.Name = request.Name ?? existing.Name;
            existing.Slug = request.Slug ?? existing.Slug;
            existing.Domain = request.Domain ?? existing.Domain;
            existing.UpdatedAt = DateTime.UtcNow;

            var updated = await _siteService.UpdateAsync(existing);
            return Ok(new { data = updated });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpDelete("{id}")]
    [EndpointSummary("Delete a site")]
    [EndpointDescription("Permanently deletes a site and all its associated content, categories, media, and settings.")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
        if (!Guid.TryParse(id, out var parsedId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid site ID." } });

        await _siteService.DeleteAsync(parsedId);
        return NoContent();
    }

    [HttpPost("{id}/set-primary")]
    [EndpointSummary("Set site as primary")]
    [EndpointDescription("Designates the specified site as the primary site. The primary site is used as the default for content delivery endpoints.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SetPrimary(string id)
    {
        if (!Guid.TryParse(id, out var parsedId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid site ID." } });

        var site = await _siteService.GetByIdAsync(parsedId);
        if (site == null)
            return NotFound();

        // Clear primary on all sites, then set this one
        var allSites = await _siteService.GetAllAsync(page: 1, pageSize: 100);
        foreach (var s in allSites.Where(s => s.IsPrimary))
        {
            s.IsPrimary = false;
            await _siteService.UpdateAsync(s);
        }

        site.IsPrimary = true;
        site.UpdatedAt = DateTime.UtcNow;
        var updated = await _siteService.UpdateAsync(site);
        return Ok(new { data = updated });
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("app_user_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

public class CreateSiteRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Domain { get; set; }
}

public class UpdateSiteRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Domain { get; set; }
}
