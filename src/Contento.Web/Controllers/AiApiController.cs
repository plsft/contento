using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Controllers;

[Tags("AI")]
[ApiController]
[Route("api/v1/ai")]
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
public class AiApiController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly ISiteService _siteService;

    public AiApiController(IAiService aiService, ISiteService siteService)
    {
        _aiService = aiService;
        _siteService = siteService;
    }

    [HttpPost("complete")]
    [EndpointSummary("AI text completion")]
    [EndpointDescription("Sends a prompt to the configured AI provider and returns the generated text response. Requires AI to be enabled in site settings.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Complete([FromBody] AiCompleteRequest request)
    {
        var settings = await LoadAiSettings();
        if (settings == null || !settings.Enabled)
            return BadRequest(new { error = new { code = "AI_DISABLED", message = "AI is not configured. Go to Settings > AI to set up your API key." } });

        var result = await _aiService.CompleteAsync(
            request.SystemPrompt ?? "You are a helpful assistant.",
            request.UserPrompt ?? "",
            settings);

        if (result.Success)
            return Ok(new { data = new { text = result.Text, tokensUsed = result.TokensUsed } });

        return BadRequest(new { error = new { code = "AI_ERROR", message = result.Error } });
    }

    [HttpPost("stream")]
    [EndpointSummary("AI streaming completion")]
    [EndpointDescription("Streams AI-generated text as Server-Sent Events (SSE) for real-time display. Sends chunks as they are generated.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task Stream([FromBody] AiCompleteRequest request, CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";

        var settings = await LoadAiSettings();
        if (settings == null || !settings.Enabled)
        {
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(new { error = "AI is not configured." })}\n\n", ct);
            await Response.Body.FlushAsync(ct);
            return;
        }

        var system = request.SystemPrompt ?? "You are a helpful writing assistant. Respond in markdown only, no preamble.";

        await foreach (var chunk in _aiService.StreamAsync(system, request.UserPrompt ?? "", settings, ct))
        {
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(new { text = chunk })}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }

        await Response.WriteAsync("data: [DONE]\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }

    [HttpPost("generate-theme")]
    [EndpointSummary("Generate a theme with AI")]
    [EndpointDescription("Uses AI to generate a complete CSS theme based on a natural language description. Returns CSS variables and theme configuration.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GenerateTheme([FromBody] AiGenerateRequest request)
    {
        var settings = await LoadAiSettings();
        if (settings == null || !settings.Enabled)
            return BadRequest(new { error = new { code = "AI_DISABLED", message = "AI is not configured." } });

        var theme = await _aiService.GenerateThemeAsync(request.Prompt ?? "", settings);
        if (theme == null)
            return BadRequest(new { error = new { code = "GENERATION_FAILED", message = "Failed to generate theme. Try a different prompt." } });

        return Ok(new { data = theme });
    }

    [HttpPost("generate-layout")]
    [EndpointSummary("Generate a layout with AI")]
    [EndpointDescription("Uses AI to generate a page layout structure based on a natural language description. Returns a layout JSON configuration.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GenerateLayout([FromBody] AiGenerateRequest request)
    {
        var settings = await LoadAiSettings();
        if (settings == null || !settings.Enabled)
            return BadRequest(new { error = new { code = "AI_DISABLED", message = "AI is not configured." } });

        var siteId = HttpContext.GetCurrentSiteId();

        var layout = await _aiService.GenerateLayoutAsync(request.Prompt ?? "", siteId, settings);
        if (layout == null)
            return BadRequest(new { error = new { code = "GENERATION_FAILED", message = "Failed to generate layout. Try a different prompt." } });

        return Ok(new { data = layout });
    }

    private Task<AiSettings?> LoadAiSettings()
    {
        var site = HttpContext.TryGetCurrentSite();
        if (site == null || string.IsNullOrWhiteSpace(site.Settings) || site.Settings == "{}")
            return Task.FromResult<AiSettings?>(null);

        try
        {
            var settings = JsonDocument.Parse(site.Settings);
            if (settings.RootElement.TryGetProperty("ai", out var aiElement))
            {
                var result = JsonSerializer.Deserialize<AiSettings>(aiElement.GetRawText(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return Task.FromResult(result);
            }
        }
        catch { }

        return Task.FromResult<AiSettings?>(null);
    }
}

public class AiCompleteRequest
{
    public string? SystemPrompt { get; set; }
    public string? UserPrompt { get; set; }
}

public class AiGenerateRequest
{
    public string? Prompt { get; set; }
}
