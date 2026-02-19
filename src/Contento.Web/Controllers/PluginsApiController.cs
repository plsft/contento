using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Controllers;

/// <summary>
/// REST API for plugin installation and management
/// </summary>
[Tags("Plugins")]
[ApiController]
[Route("api/v1/plugins")]
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
public class PluginsApiController : ControllerBase
{
    private readonly IPluginService _pluginService;
    private readonly ISiteService _siteService;

    public PluginsApiController(IPluginService pluginService, ISiteService siteService)
    {
        _pluginService = pluginService;
        _siteService = siteService;
    }

    [HttpGet]
    [EndpointSummary("List installed plugins")]
    [EndpointDescription("Returns a paginated list of installed plugins for the current site with optional filtering by enabled status.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> List(
        [FromQuery] bool? enabledOnly = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var plugins = await _pluginService.GetAllBySiteAsync(siteId, enabledOnly, page, pageSize);
        var total = await _pluginService.GetTotalCountAsync(siteId, enabledOnly);

        return Ok(new
        {
            data = plugins,
            meta = new { page, pageSize, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    [HttpPost("install")]
    [EndpointSummary("Install a plugin")]
    [EndpointDescription("Installs a new plugin for the current site. The plugin is registered but not automatically enabled.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Install([FromBody] InstalledPlugin plugin)
    {
        try
        {
            var siteId = HttpContext.GetCurrentSiteId();
            plugin.SiteId = siteId;

            var installed = await _pluginService.InstallAsync(plugin);
            return CreatedAtAction(nameof(GetById), new { id = installed.Id }, new { data = installed });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpGet("{id}")]
    [EndpointSummary("Get a plugin by ID")]
    [EndpointDescription("Returns the full details of an installed plugin including its configuration, version, and enabled status.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(string id)
    {
        if (!Guid.TryParse(id, out var pluginId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid plugin ID." } });

        var plugin = await _pluginService.GetByIdAsync(pluginId);
        if (plugin == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Plugin not found." } });

        return Ok(new { data = plugin });
    }

    [HttpPost("{id}/enable")]
    [EndpointSummary("Enable a plugin")]
    [EndpointDescription("Enables a previously installed plugin, allowing it to execute its hooks and modify site behavior.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Enable(string id)
    {
        if (!Guid.TryParse(id, out var pluginId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid plugin ID." } });

        var plugin = await _pluginService.GetByIdAsync(pluginId);
        if (plugin == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Plugin not found." } });

        await _pluginService.EnableAsync(pluginId);
        var updated = await _pluginService.GetByIdAsync(pluginId);
        return Ok(new { data = updated });
    }

    [HttpPost("{id}/disable")]
    [EndpointSummary("Disable a plugin")]
    [EndpointDescription("Disables an installed plugin without uninstalling it. The plugin's hooks will no longer execute.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Disable(string id)
    {
        if (!Guid.TryParse(id, out var pluginId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid plugin ID." } });

        var plugin = await _pluginService.GetByIdAsync(pluginId);
        if (plugin == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Plugin not found." } });

        await _pluginService.DisableAsync(pluginId);
        var updated = await _pluginService.GetByIdAsync(pluginId);
        return Ok(new { data = updated });
    }

    [HttpPut("{id}/settings")]
    [EndpointSummary("Update plugin settings")]
    [EndpointDescription("Updates the JSON configuration settings for a specific plugin. Replaces the entire settings object.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateSettings(string id, [FromBody] UpdatePluginSettingsRequest request)
    {
        if (!Guid.TryParse(id, out var pluginId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid plugin ID." } });

        var plugin = await _pluginService.GetByIdAsync(pluginId);
        if (plugin == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Plugin not found." } });

        try
        {
            await _pluginService.UpdateSettingsAsync(pluginId, request.Settings ?? "{}");
            var settings = await _pluginService.GetSettingsAsync(pluginId);
            return Ok(new { data = new { settings } });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpDelete("{id}")]
    [EndpointSummary("Uninstall a plugin")]
    [EndpointDescription("Completely removes an installed plugin and its settings from the current site.")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Uninstall(string id)
    {
        if (!Guid.TryParse(id, out var pluginId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid plugin ID." } });

        var existing = await _pluginService.GetByIdAsync(pluginId);
        if (existing == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Plugin not found." } });

        await _pluginService.UninstallAsync(pluginId);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("app_user_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

public class UpdatePluginSettingsRequest
{
    public string? Settings { get; set; }
}
