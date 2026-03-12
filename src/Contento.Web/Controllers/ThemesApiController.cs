using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Web.Controllers;

[Tags("Themes")]
[ApiController]
[Route("api/v1/themes")]
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
public class ThemesApiController : ControllerBase
{
    private readonly IThemeService _themeService;

    public ThemesApiController(IThemeService themeService)
    {
        _themeService = themeService;
    }

    [HttpGet]
    [EndpointSummary("List all themes")]
    [EndpointDescription("Returns a paginated list of all available themes with their CSS variables and activation status.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var themes = await _themeService.GetAllAsync(page, pageSize);
        var themeList = themes.ToList();
        return Ok(new
        {
            data = themeList,
            meta = new { page, pageSize, totalCount = themeList.Count, totalPages = 1 }
        });
    }

    [HttpGet("{id}")]
    [EndpointSummary("Get a theme by ID")]
    [EndpointDescription("Returns the full details of a specific theme including its CSS variables and metadata.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get(string id)
    {
        if (!Guid.TryParse(id, out var themeId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid theme ID." } });

        var theme = await _themeService.GetByIdAsync(themeId);
        if (theme == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Theme not found." } });

        return Ok(new { data = theme });
    }

    [HttpPost]
    [EndpointSummary("Create a new theme")]
    [EndpointDescription("Creates a new theme with the specified name, CSS variables, and metadata. The theme is created in an inactive state.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateThemeRequest request)
    {
        try
        {
            var theme = new Theme
            {
                Name = request.Name ?? "Untitled",
                Slug = request.Slug ?? "",
                Description = request.Description,
                Version = request.Version ?? "1.0.0",
                Author = request.Author,
                CssVariables = request.CssVariables ?? "{}",
                IsActive = false
            };

            var created = await _themeService.CreateAsync(theme);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, new { data = created });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpDelete("{id}")]
    [EndpointSummary("Delete a theme")]
    [EndpointDescription("Permanently deletes a theme by its ID. Active themes cannot be deleted.")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
        if (!Guid.TryParse(id, out var themeId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid theme ID." } });

        var existing = await _themeService.GetByIdAsync(themeId);
        if (existing == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Theme not found." } });

        try
        {
            await _themeService.DeleteAsync(themeId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpPost("{id}/activate")]
    [EndpointSummary("Activate a theme")]
    [EndpointDescription("Sets the specified theme as the active theme for the site. Deactivates any previously active theme.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Activate(string id)
    {
        if (!Guid.TryParse(id, out var themeId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid theme ID." } });

        var theme = await _themeService.GetByIdAsync(themeId);
        if (theme == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Theme not found." } });

        await _themeService.ActivateAsync(themeId);
        var updated = await _themeService.GetByIdAsync(themeId);
        return Ok(new { data = updated });
    }
}

public class CreateThemeRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? Version { get; set; }
    public string? Author { get; set; }
    public string? CssVariables { get; set; }
}
