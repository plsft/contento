using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Web.Middleware;

namespace Contento.Web.Controllers;

/// <summary>
/// REST API for content import and export operations.
/// Supports WordPress WXR import, markdown import, and JSON/HTML/markdown export.
/// </summary>
[Tags("Import/Export")]
[ApiController]
[Route("api/v1/import-export")]
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
public class ImportExportApiController : ControllerBase
{
    private readonly IImportExportService _importExportService;
    private readonly ISiteService _siteService;

    public ImportExportApiController(IImportExportService importExportService, ISiteService siteService)
    {
        _importExportService = importExportService;
        _siteService = siteService;
    }

    /// <summary>
    /// Import content from a WordPress WXR file.
    /// </summary>
    [HttpPost("wordpress")]
    [EndpointSummary("Import from WordPress")]
    [EndpointDescription("Imports content from a WordPress WXR export file (.xml or .wxr). Converts posts, pages, and categories to Contento format.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ImportWordPress([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = new { code = "MISSING_FILE", message = "No WXR file provided." } });

        if (!file.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) &&
            !file.FileName.EndsWith(".wxr", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = new { code = "INVALID_FILE", message = "File must be .xml or .wxr format." } });

        var userId = GetCurrentUserId();
        var siteId = HttpContext.GetCurrentSiteId();

        await using var stream = file.OpenReadStream();
        var result = await _importExportService.ImportWordPressAsync(siteId, stream, userId);

        return Ok(new { data = result });
    }

    /// <summary>
    /// Import posts from uploaded markdown files.
    /// </summary>
    [HttpPost("markdown")]
    [EndpointSummary("Import markdown files")]
    [EndpointDescription("Imports posts from uploaded markdown files with optional YAML front matter. Each .md file becomes a new post.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ImportMarkdown([FromForm] IFormFileCollection files)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { error = new { code = "MISSING_FILES", message = "No markdown files provided." } });

        var userId = GetCurrentUserId();
        var siteId = HttpContext.GetCurrentSiteId();

        var markdownFiles = new List<(string Filename, string Content)>();
        foreach (var file in files)
        {
            if (!file.FileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase))
                continue;

            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();
            markdownFiles.Add((file.FileName, content));
        }

        if (markdownFiles.Count == 0)
            return BadRequest(new { error = new { code = "NO_MARKDOWN", message = "No .md files found in upload." } });

        var result = await _importExportService.ImportMarkdownAsync(siteId, markdownFiles, userId);

        return Ok(new { data = result });
    }

    /// <summary>
    /// Export full site as JSON.
    /// </summary>
    [HttpGet("site")]
    [EndpointSummary("Export site as JSON")]
    [EndpointDescription("Exports the entire site content as a JSON document including posts, categories, settings, and metadata.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ExportSite()
    {
        var siteId = HttpContext.GetCurrentSiteId();

        var json = await _importExportService.ExportSiteJsonAsync(siteId);

        return Content(json, "application/json");
    }

    /// <summary>
    /// Export a single post as markdown with front matter.
    /// </summary>
    [HttpGet("posts/{id}/markdown")]
    [EndpointSummary("Export post as markdown")]
    [EndpointDescription("Exports a single post as a markdown file with YAML front matter containing title, date, tags, and other metadata.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ExportPostMarkdown(string id)
    {
        if (!Guid.TryParse(id, out var postId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid post ID." } });

        try
        {
            var markdown = await _importExportService.ExportPostMarkdownAsync(postId);
            return Content(markdown, "text/markdown");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Post not found." } });
        }
    }

    /// <summary>
    /// Export a single post as HTML.
    /// </summary>
    [HttpGet("posts/{id}/html")]
    [EndpointSummary("Export post as HTML")]
    [EndpointDescription("Exports a single post as rendered HTML content, suitable for static publishing or embedding.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ExportPostHtml(string id)
    {
        if (!Guid.TryParse(id, out var postId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid post ID." } });

        try
        {
            var html = await _importExportService.ExportPostHtmlAsync(postId);
            return Content(html, "text/html");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Post not found." } });
        }
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("app_user_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
