using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Controllers;

/// <summary>
/// REST API for post management (used by editor and external integrations)
/// </summary>
[Tags("Posts")]
[ApiController]
[Route("api/v1/posts")]
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
public class PostsApiController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly ISiteService _siteService;
    private readonly IMarkdownService _markdownService;
    private readonly ISeoService? _seoService;
    private readonly IThemeService? _themeService;

    public PostsApiController(IPostService postService, ISiteService siteService, IMarkdownService markdownService,
        ISeoService? seoService = null, IThemeService? themeService = null)
    {
        _postService = postService;
        _siteService = siteService;
        _markdownService = markdownService;
        _seoService = seoService;
        _themeService = themeService;
    }

    [HttpGet]
    [EndpointSummary("List all posts")]
    [EndpointDescription("Returns a paginated list of posts for the current site with optional filtering by status and text search.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> List(
        [FromQuery] string? status = null,
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var posts = await _postService.GetAllAsync(siteId, status: status, search: q, page: page, pageSize: pageSize);
        var total = await _postService.GetTotalCountAsync(siteId, status);

        return Ok(new
        {
            data = posts,
            meta = new { page, pageSize, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    [HttpGet("{id}")]
    [EndpointSummary("Get a post by ID")]
    [EndpointDescription("Returns the full details of a specific post including its markdown body, metadata, and SEO fields.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get(string id)
    {
        if (!Guid.TryParse(id, out var postId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid post ID." } });

        var post = await _postService.GetByIdAsync(postId);
        if (post == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Post not found." } });

        return Ok(new { data = post });
    }

    [HttpGet("by-slug/{slug}")]
    [EndpointSummary("Get a post by slug")]
    [EndpointDescription("Returns the full details of a post identified by its URL slug for the current site.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var post = await _postService.GetBySlugAsync(siteId, slug);
        if (post == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Post not found." } });

        return Ok(new { data = post });
    }

    [HttpPost]
    [EndpointSummary("Create a post")]
    [EndpointDescription("Creates a new post with the specified title, body, status, and metadata. Defaults to draft status if not specified.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreatePostRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var siteId = HttpContext.GetCurrentSiteId();

            var post = new Post
            {
                SiteId = siteId,
                Title = request.Title ?? "Untitled",
                Subtitle = request.Subtitle,
                BodyMarkdown = request.BodyMarkdown ?? "",
                CoverImageUrl = request.CoverImageUrl,
                AuthorId = userId,
                Status = request.Status ?? "draft",
                Visibility = request.Visibility ?? "public",
                Featured = request.Featured,
                MetaTitle = request.MetaTitle,
                MetaDescription = request.MetaDescription,
                CanonicalUrl = request.CanonicalUrl,
                Tags = request.Tags
            };

            var created = await _postService.CreateAsync(post);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, new { data = created });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpPut("{id}")]
    [EndpointSummary("Update a post")]
    [EndpointDescription("Updates an existing post's title, body, status, SEO fields, or other metadata. Only provided fields are modified. Automatically sets publishedAt when transitioning to published.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePostRequest request)
    {
        if (!Guid.TryParse(id, out var postId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid post ID." } });

        try
        {
            var existing = await _postService.GetByIdAsync(postId);
            if (existing == null)
                return NotFound(new { error = new { code = "NOT_FOUND", message = "Post not found." } });

            var userId = GetCurrentUserId();

            if (request.Title != null) existing.Title = request.Title;
            if (request.Subtitle != null) existing.Subtitle = request.Subtitle;
            if (request.Slug != null) existing.Slug = request.Slug;
            if (request.BodyMarkdown != null) existing.BodyMarkdown = request.BodyMarkdown;
            if (request.CoverImageUrl != null) existing.CoverImageUrl = request.CoverImageUrl;
            if (request.Status != null)
            {
                if (request.Status == "published" && existing.Status != "published")
                    existing.PublishedAt = DateTime.UtcNow;
                existing.Status = request.Status;
            }
            if (request.Visibility != null) existing.Visibility = request.Visibility;
            if (request.Featured.HasValue) existing.Featured = request.Featured.Value;
            if (request.MetaTitle != null) existing.MetaTitle = request.MetaTitle;
            if (request.MetaDescription != null) existing.MetaDescription = request.MetaDescription;
            if (request.CanonicalUrl != null) existing.CanonicalUrl = request.CanonicalUrl;
            if (request.Tags != null) existing.Tags = request.Tags;

            var updated = await _postService.UpdateAsync(existing, userId);
            return Ok(new { data = updated });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpDelete("{id}")]
    [EndpointSummary("Delete a post")]
    [EndpointDescription("Permanently deletes a post and its associated revision history by ID.")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
        if (!Guid.TryParse(id, out var postId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid post ID." } });

        var existing = await _postService.GetByIdAsync(postId);
        if (existing == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Post not found." } });

        await _postService.DeleteAsync(postId);
        return NoContent();
    }

    [HttpPost("{id}/publish")]
    [EndpointSummary("Publish a post")]
    [EndpointDescription("Transitions a draft or scheduled post to published status and sets the publication timestamp to now.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Publish(string id)
    {
        if (!Guid.TryParse(id, out var postId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid post ID." } });

        var post = await _postService.GetByIdAsync(postId);
        if (post == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Post not found." } });

        await _postService.PublishAsync(postId);
        var updated = await _postService.GetByIdAsync(postId);
        return Ok(new { data = updated });
    }

    [HttpPost("{id}/unpublish")]
    [EndpointSummary("Unpublish a post")]
    [EndpointDescription("Reverts a published post back to draft status, removing it from the public site.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Unpublish(string id)
    {
        if (!Guid.TryParse(id, out var postId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid post ID." } });

        var post = await _postService.GetByIdAsync(postId);
        if (post == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Post not found." } });

        await _postService.UnpublishAsync(postId);
        var updated = await _postService.GetByIdAsync(postId);
        return Ok(new { data = updated });
    }

    /// <summary>
    /// Live preview — renders Markdown to HTML wrapped in site theme.
    /// </summary>
    [HttpPost("preview")]
    [EndpointSummary("Preview post content")]
    [EndpointDescription("Renders markdown content to HTML with the site's active theme CSS, producing a full preview page with title, subtitle, and cover image.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Preview([FromBody] PreviewRequest request)
    {
        var bodyHtml = _markdownService.RenderToHtml(request.BodyMarkdown ?? "");

        // Build full preview HTML with theme CSS
        string themeCss = "";
        try
        {
            var site = HttpContext.GetCurrentSite();
            if (site.ThemeId != null && _themeService != null)
            {
                var theme = await _themeService.GetByIdAsync(site.ThemeId.Value);
                if (theme?.CssVariables != null)
                {
                    var vars = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(theme.CssVariables);
                    if (vars != null)
                    {
                        var sb = new System.Text.StringBuilder(":root { ");
                        foreach (var kv in vars) sb.Append($"{kv.Key}: {kv.Value}; ");
                        sb.Append('}');
                        themeCss = sb.ToString();
                    }
                }
            }
        }
        catch { /* ignore theme loading errors */ }

        var customCssBlock = string.IsNullOrEmpty(request.CustomCss) ? "" : $"<style>{request.CustomCss}</style>";
        var titleHtml = string.IsNullOrEmpty(request.Title) ? "" : $"<h1 style=\"font-size: 2.5rem; font-weight: 700; margin-bottom: 0.5rem;\">{System.Net.WebUtility.HtmlEncode(request.Title)}</h1>";
        var subtitleHtml = string.IsNullOrEmpty(request.Subtitle) ? "" : $"<p style=\"font-size: 1.25rem; color: #666; margin-bottom: 2rem;\">{System.Net.WebUtility.HtmlEncode(request.Subtitle)}</p>";
        var coverHtml = string.IsNullOrEmpty(request.CoverImageUrl) ? "" : $"<img src=\"{System.Net.WebUtility.HtmlEncode(request.CoverImageUrl)}\" style=\"width: 100%; max-height: 400px; object-fit: cover; border-radius: 8px; margin-bottom: 2rem;\" />";

        var fullHtml = $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8" />
                <link rel="stylesheet" href="/css/app.css" />
                <style>{{themeCss}}</style>
                <style>body { max-width: 720px; margin: 0 auto; padding: 2rem; font-family: Georgia, serif; }</style>
                {{customCssBlock}}
            </head>
            <body class="prose max-w-none">
                {{coverHtml}}
                {{titleHtml}}
                {{subtitleHtml}}
                {{bodyHtml}}
            </body>
            </html>
            """;

        return Ok(new { data = new { bodyHtml, fullHtml } });
    }

    /// <summary>
    /// SEO analysis for a post.
    /// </summary>
    [HttpPost("{id}/seo-analyze")]
    [EndpointSummary("Analyze post SEO")]
    [EndpointDescription("Performs SEO analysis on a post for a given focus keyword, returning an overall score and a list of actionable improvement suggestions.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SeoAnalyze(string id, [FromBody] SeoAnalyzeRequest request)
    {
        if (!Guid.TryParse(id, out var postId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid post ID." } });

        if (_seoService == null)
            return Ok(new { data = new { overallScore = 0, issues = "[]" } });

        var analysis = await _seoService.AnalyzePostAsync(postId, request.FocusKeyword);
        return Ok(new { data = analysis });
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("app_user_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

public class CreatePostRequest
{
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? BodyMarkdown { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Status { get; set; }
    public string? Visibility { get; set; }
    public bool Featured { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? CanonicalUrl { get; set; }
    public string[]? Tags { get; set; }
}

public class UpdatePostRequest
{
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Slug { get; set; }
    public string? BodyMarkdown { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Status { get; set; }
    public string? Visibility { get; set; }
    public bool? Featured { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? CanonicalUrl { get; set; }
    public string[]? Tags { get; set; }
}

public class PreviewRequest
{
    public string? BodyMarkdown { get; set; }
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? CustomCss { get; set; }
}

public class SeoAnalyzeRequest
{
    public string? FocusKeyword { get; set; }
}
