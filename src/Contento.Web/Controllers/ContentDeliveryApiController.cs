using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Web.Middleware;

namespace Contento.Web.Controllers;

/// <summary>
/// Headless content delivery API — read-only endpoints optimized for frontend consumption.
/// All endpoints are public (no auth required) and designed for static site generators,
/// mobile apps, SPAs, and other headless consumers.
/// </summary>
[Tags("Content Delivery")]
[ApiController]
[Route("api/v1/content")]
[AllowAnonymous]
public class ContentDeliveryApiController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly ICategoryService _categoryService;
    private readonly IThemeService _themeService;
    private readonly ILayoutService _layoutService;
    private readonly ISiteService _siteService;
    private readonly ISearchService _searchService;
    private readonly ICommentService _commentService;
    private readonly IMarkdownService _markdownService;
    private readonly IMediaService _mediaService;
    private readonly IMenuService _menuService;

    public ContentDeliveryApiController(
        IPostService postService,
        ICategoryService categoryService,
        IThemeService themeService,
        ILayoutService layoutService,
        ISiteService siteService,
        ISearchService searchService,
        ICommentService commentService,
        IMarkdownService markdownService,
        IMediaService mediaService,
        IMenuService menuService)
    {
        _postService = postService;
        _categoryService = categoryService;
        _themeService = themeService;
        _layoutService = layoutService;
        _siteService = siteService;
        _searchService = searchService;
        _commentService = commentService;
        _markdownService = markdownService;
        _mediaService = mediaService;
        _menuService = menuService;
    }

    // ─── Site ──────────────────────────────────────────

    /// <summary>
    /// Get site metadata, active theme, and navigation.
    /// GET /api/v1/content/site
    /// </summary>
    [HttpGet("site")]
    [ResponseCache(Duration = 300)]
    [EndpointSummary("Get site metadata")]
    [EndpointDescription("Returns the site configuration, active theme, default layout, and category tree for frontend consumption. Cached for 5 minutes.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSite()
    {
        var site = HttpContext.TryGetCurrentSite();
        if (site == null)
            return NotFound(new { error = new { code = "NO_SITE", message = "No site configured." } });

        var theme = await _themeService.GetActiveAsync();
        var categories = await _categoryService.GetTreeAsync(site.Id);
        var defaultLayout = await _layoutService.GetDefaultAsync(site.Id);

        return Ok(new
        {
            data = new
            {
                site = new
                {
                    site.Id,
                    site.Name,
                    site.Slug,
                    site.Tagline,
                    site.Locale,
                    site.Timezone,
                    site.Domain,
                    site.Settings
                },
                theme = theme == null ? null : new
                {
                    theme.Id,
                    theme.Name,
                    theme.Slug,
                    theme.CssVariables
                },
                defaultLayout = defaultLayout == null ? null : new
                {
                    defaultLayout.Id,
                    defaultLayout.Name,
                    defaultLayout.Slug,
                    defaultLayout.Structure
                },
                navigation = categories
            }
        });
    }

    // ─── Posts ─────────────────────────────────────────

    /// <summary>
    /// List published posts with pagination and optional filters.
    /// GET /api/v1/content/posts
    /// </summary>
    [HttpGet("posts")]
    [ResponseCache(Duration = 60)]
    [EndpointSummary("List published posts")]
    [EndpointDescription("Returns a paginated list of published posts with optional filtering by category slug, tag, or featured status. Cached for 1 minute.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ListPosts(
        [FromQuery] string? category = null,
        [FromQuery] string? tag = null,
        [FromQuery] bool? featured = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var site = GetDefaultSite();
        if (site == null) return SiteNotFound();

        Guid? categoryId = null;
        if (!string.IsNullOrEmpty(category))
        {
            var cat = await _categoryService.GetBySlugAsync(site.Id, category);
            if (cat != null) categoryId = cat.Id;
        }

        var posts = await _postService.GetAllAsync(site.Id, "published", categoryId, tag, null, page, pageSize);
        var total = await _postService.GetTotalCountAsync(site.Id, "published", categoryId, tag);

        var result = posts.Select(p => new
        {
            p.Id,
            p.Title,
            p.Subtitle,
            p.Slug,
            p.Excerpt,
            p.CoverImageUrl,
            p.Status,
            p.Featured,
            p.PublishedAt,
            p.WordCount,
            p.ReadingTimeMinutes,
            p.Tags,
            author = new { p.AuthorId }
        });

        if (featured == true)
            result = result.Where(p => p.Featured);

        return Ok(new
        {
            data = result,
            meta = new
            {
                page,
                pageSize,
                totalCount = total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }

    /// <summary>
    /// Get a single published post by slug with full rendered HTML.
    /// GET /api/v1/content/posts/{slug}
    /// </summary>
    [HttpGet("posts/{slug}")]
    [ResponseCache(Duration = 60)]
    [EndpointSummary("Get a published post by slug")]
    [EndpointDescription("Returns a single published post with rendered HTML body, table of contents, and comment count. Cached for 1 minute.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPost(string slug)
    {
        var site = GetDefaultSite();
        if (site == null) return SiteNotFound();

        var post = await _postService.GetBySlugAsync(site.Id, slug);
        if (post == null || post.Status != "published")
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Post not found." } });

        // Render markdown to HTML if body html is empty
        var bodyHtml = !string.IsNullOrEmpty(post.BodyHtml) ? post.BodyHtml
            : !string.IsNullOrEmpty(post.BodyMarkdown) ? _markdownService.RenderToHtml(post.BodyMarkdown)
            : "";

        var (htmlWithToc, headings) = !string.IsNullOrEmpty(post.BodyMarkdown)
            ? _markdownService.RenderWithTableOfContents(post.BodyMarkdown)
            : (bodyHtml, Enumerable.Empty<(string Id, string Text, int Level)>());

        var commentCount = await _commentService.GetCountByPostAsync(post.Id, "approved");

        return Ok(new
        {
            data = new
            {
                post.Id,
                post.Title,
                post.Subtitle,
                post.Slug,
                post.Excerpt,
                post.BodyMarkdown,
                bodyHtml,
                post.CoverImageUrl,
                post.MetaTitle,
                post.MetaDescription,
                post.CanonicalUrl,
                post.Status,
                post.Visibility,
                post.Featured,
                post.PublishedAt,
                post.WordCount,
                post.ReadingTimeMinutes,
                post.Tags,
                post.Version,
                author = new { post.AuthorId },
                commentCount,
                tableOfContents = headings.Select(h => new { h.Id, h.Text, h.Level })
            }
        });
    }

    // ─── Categories ───────────────────────────────────

    /// <summary>
    /// Get category tree for navigation.
    /// GET /api/v1/content/categories
    /// </summary>
    [HttpGet("categories")]
    [ResponseCache(Duration = 300)]
    [EndpointSummary("List categories for navigation")]
    [EndpointDescription("Returns the category tree structure for the site, suitable for building navigation menus. Cached for 5 minutes.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ListCategories()
    {
        var site = GetDefaultSite();
        if (site == null) return SiteNotFound();

        var categories = await _categoryService.GetTreeAsync(site.Id);
        return Ok(new { data = categories });
    }

    // ─── Comments ─────────────────────────────────────

    /// <summary>
    /// Get approved comments for a post (threaded).
    /// GET /api/v1/content/posts/{slug}/comments
    /// </summary>
    [HttpGet("posts/{slug}/comments")]
    [ResponseCache(Duration = 30)]
    [EndpointSummary("Get post comments")]
    [EndpointDescription("Returns approved threaded comments for a post identified by slug. Cached for 30 seconds.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetComments(string slug)
    {
        var site = GetDefaultSite();
        if (site == null) return SiteNotFound();

        var post = await _postService.GetBySlugAsync(site.Id, slug);
        if (post == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Post not found." } });

        var comments = await _commentService.GetThreadedByPostAsync(post.Id);
        return Ok(new { data = comments });
    }

    /// <summary>
    /// Submit a comment on a post.
    /// POST /api/v1/content/posts/{slug}/comments
    /// </summary>
    [HttpPost("posts/{slug}/comments")]
    [EndpointSummary("Submit a comment on a post")]
    [EndpointDescription("Submits a new comment on a published post. Includes honeypot spam protection. Comments start in pending status for moderation.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SubmitComment(string slug, [FromBody] SubmitCommentRequest request)
    {
        var site = GetDefaultSite();
        if (site == null) return SiteNotFound();

        var post = await _postService.GetBySlugAsync(site.Id, slug);
        if (post == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Post not found." } });

        // Honeypot check
        if (!string.IsNullOrEmpty(request.Website))
            return Ok(new { data = new { status = "pending" } }); // silently discard spam

        if (string.IsNullOrWhiteSpace(request.AuthorName))
            return BadRequest(new { error = new { code = "VALIDATION", message = "Author name is required." } });

        if (string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new { error = new { code = "VALIDATION", message = "Comment body is required." } });

        var comment = new Core.Models.Comment
        {
            PostId = post.Id,
            AuthorName = request.AuthorName,
            AuthorEmail = request.AuthorEmail ?? "",
            BodyMarkdown = request.Body,
            BodyHtml = _markdownService.RenderCommentToHtml(request.Body),
            ParentId = request.ParentId,
            Status = "pending",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        var created = await _commentService.CreateAsync(comment);
        return Ok(new { data = new { id = created.Id, status = "pending" } });
    }

    // ─── Search ───────────────────────────────────────

    /// <summary>
    /// Search published content.
    /// GET /api/v1/content/search
    /// </summary>
    [HttpGet("search")]
    [EndpointSummary("Search published content")]
    [EndpointDescription("Performs a full-text search across published content with highlighted title and excerpt matches.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Search(
        [FromQuery] string q = "",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new { data = Array.Empty<object>(), meta = new { page, pageSize, totalCount = 0, totalPages = 0 } });

        var site = GetDefaultSite();
        if (site == null) return SiteNotFound();

        var results = await _searchService.SearchWithHighlightsAsync(site.Id, q, page, pageSize);
        var total = await _searchService.GetSearchResultCountAsync(site.Id, q);

        return Ok(new
        {
            data = results.Select(r => new
            {
                r.Post.Id,
                r.Post.Title,
                r.Post.Slug,
                r.Post.Excerpt,
                r.Post.CoverImageUrl,
                r.Post.PublishedAt,
                r.Post.ReadingTimeMinutes,
                r.Post.Tags,
                highlightedTitle = r.HighlightedTitle,
                highlightedExcerpt = r.HighlightedExcerpt
            }),
            meta = new
            {
                page,
                pageSize,
                totalCount = total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }

    /// <summary>
    /// Get search suggestions / autocomplete.
    /// GET /api/v1/content/search/suggestions
    /// </summary>
    [HttpGet("search/suggestions")]
    [EndpointSummary("Get search autocomplete suggestions")]
    [EndpointDescription("Returns typeahead search suggestions for the public site search, limited to the specified number of results.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Suggestions([FromQuery] string q = "", [FromQuery] int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new { data = Array.Empty<string>() });

        var site = GetDefaultSite();
        if (site == null) return SiteNotFound();

        var suggestions = await _searchService.GetSuggestionsAsync(site.Id, q, limit);
        return Ok(new { data = suggestions });
    }

    // ─── Themes ───────────────────────────────────────

    /// <summary>
    /// Get available themes.
    /// GET /api/v1/content/themes
    /// </summary>
    [HttpGet("themes")]
    [ResponseCache(Duration = 300)]
    [EndpointSummary("List available themes")]
    [EndpointDescription("Returns all themes with their CSS variables and activation status. Cached for 5 minutes.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ListThemes()
    {
        var themes = await _themeService.GetAllAsync();
        return Ok(new
        {
            data = themes.Select(t => new
            {
                t.Id,
                t.Name,
                t.Slug,
                t.Description,
                t.CssVariables,
                t.IsActive
            })
        });
    }

    // ─── Layouts ──────────────────────────────────────

    /// <summary>
    /// Get available layouts.
    /// GET /api/v1/content/layouts
    /// </summary>
    [HttpGet("layouts")]
    [ResponseCache(Duration = 300)]
    [EndpointSummary("List available layouts")]
    [EndpointDescription("Returns all layout templates with their structure and default status. Cached for 5 minutes.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ListLayouts()
    {
        var site = GetDefaultSite();
        if (site == null) return SiteNotFound();

        var layouts = await _layoutService.GetAllBySiteAsync(site.Id);
        return Ok(new
        {
            data = layouts.Select(l => new
            {
                l.Id,
                l.Name,
                l.Slug,
                l.Description,
                l.Structure,
                l.IsDefault
            })
        });
    }

    // ─── Media ────────────────────────────────────────

    /// <summary>
    /// List media files.
    /// GET /api/v1/content/media
    /// </summary>
    [HttpGet("media")]
    [ResponseCache(Duration = 60)]
    [EndpointSummary("List media files")]
    [EndpointDescription("Returns a paginated list of media files with optional type filtering. Cached for 1 minute.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ListMedia(
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        var site = GetDefaultSite();
        if (site == null) return SiteNotFound();

        var media = await _mediaService.GetAllBySiteAsync(site.Id, type, null, page, pageSize);
        var total = await _mediaService.GetTotalCountAsync(site.Id, type);

        return Ok(new
        {
            data = media,
            meta = new
            {
                page,
                pageSize,
                totalCount = total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }

    // ─── Menus ────────────────────────────────────────

    /// <summary>
    /// Get the menu tree for a specific location (header, footer, etc.).
    /// GET /api/v1/content/menu/{location}
    /// </summary>
    [HttpGet("menu/{location}")]
    [ResponseCache(Duration = 300)]
    [EndpointSummary("Get menu by location")]
    [EndpointDescription("Returns the navigation menu tree for a specific location (e.g. header, footer). Cached for 5 minutes.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMenu(string location)
    {
        var site = GetDefaultSite();
        if (site == null) return SiteNotFound();

        var menu = await _menuService.GetByLocationAsync(site.Id, location);
        if (menu == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = $"No active menu found for location '{location}'." } });

        var tree = await _menuService.GetItemTreeAsync(menu.Id);
        return Ok(new
        {
            data = new
            {
                menu = new { menu.Id, menu.Name, menu.Slug, menu.Location },
                items = tree
            }
        });
    }

    // ─── Helpers ──────────────────────────────────────

    private Core.Models.Site? GetDefaultSite()
    {
        return HttpContext.TryGetCurrentSite();
    }

    private IActionResult SiteNotFound()
    {
        return NotFound(new { error = new { code = "NO_SITE", message = "No site configured." } });
    }
}

public class SubmitCommentRequest
{
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorEmail { get; set; }
    public string Body { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string? Website { get; set; } // honeypot
}
