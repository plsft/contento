using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Noundry.Guardian;

namespace Contento.Services;

/// <summary>
/// Import/export service supporting WordPress WXR import, markdown import,
/// JSON site export, and individual post export.
/// </summary>
public class ImportExportService : IImportExportService
{
    private readonly IDbConnection _db;
    private readonly IPostService _postService;
    private readonly ICategoryService _categoryService;
    private readonly ICommentService _commentService;
    private readonly IMarkdownService _markdownService;
    private readonly ISiteService _siteService;
    private readonly ILogger<ImportExportService> _logger;

    public ImportExportService(
        IDbConnection db,
        IPostService postService,
        ICategoryService categoryService,
        ICommentService commentService,
        IMarkdownService markdownService,
        ISiteService siteService,
        ILogger<ImportExportService> logger)
    {
        _db = db;
        _postService = postService;
        _categoryService = categoryService;
        _commentService = commentService;
        _markdownService = markdownService;
        _siteService = siteService;
        _logger = logger;
    }

    public async Task<ImportResult> ImportWordPressAsync(Guid siteId, Stream wxrStream, Guid importedBy)
    {
        Guard.Against.DefaultStruct(siteId);
        Guard.Against.Null(wxrStream);
        Guard.Against.DefaultStruct(importedBy);

        var sw = Stopwatch.StartNew();
        var result = new ImportResult();

        try
        {
            var doc = await XDocument.LoadAsync(wxrStream, LoadOptions.None, CancellationToken.None);
            var channel = doc.Root?.Element("channel");
            if (channel == null)
            {
                result.Errors++;
                result.ErrorMessages.Add("Invalid WXR file: missing <channel> element.");
                return result;
            }

            XNamespace wp = "http://wordpress.org/export/1.2/";
            XNamespace content = "http://purl.org/rss/1.0/modules/content/";
            XNamespace dc = "http://purl.org/dc/elements/1.1/";

            // Import categories
            var categoryMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            foreach (var catElement in channel.Elements(wp + "category"))
            {
                try
                {
                    var catName = catElement.Element(wp + "cat_name")?.Value ?? "";
                    var catSlug = catElement.Element(wp + "category_nicename")?.Value ?? "";
                    if (string.IsNullOrWhiteSpace(catName)) continue;

                    var category = new Category
                    {
                        SiteId = siteId,
                        Name = catName,
                        Slug = string.IsNullOrWhiteSpace(catSlug) ? GenerateSlug(catName) : catSlug,
                        Description = catElement.Element(wp + "category_description")?.Value
                    };
                    var created = await _categoryService.CreateAsync(category);
                    categoryMap[catSlug] = created.Id;
                    result.CategoriesImported++;
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    result.ErrorMessages.Add($"Category import error: {ex.Message}");
                }
            }

            // Import posts/pages
            foreach (var item in channel.Elements("item"))
            {
                try
                {
                    var postType = item.Element(wp + "post_type")?.Value;
                    if (postType != "post" && postType != "page") continue;

                    var title = item.Element("title")?.Value ?? "Untitled";
                    var slug = item.Element(wp + "post_name")?.Value ?? GenerateSlug(title);
                    var bodyContent = item.Element(content + "encoded")?.Value ?? "";
                    var status = item.Element(wp + "status")?.Value ?? "draft";
                    var pubDate = item.Element("pubDate")?.Value;

                    var mappedStatus = status switch
                    {
                        "publish" => "published",
                        "draft" => "draft",
                        "pending" => "review",
                        "private" => "draft",
                        "future" => "scheduled",
                        _ => "draft"
                    };

                    // Find category
                    Guid? categoryId = null;
                    var categoryElement = item.Elements("category")
                        .FirstOrDefault(c => c.Attribute("domain")?.Value == "category");
                    if (categoryElement != null)
                    {
                        var catSlug = categoryElement.Attribute("nicename")?.Value ?? "";
                        if (categoryMap.TryGetValue(catSlug, out var catId))
                            categoryId = catId;
                    }

                    // Collect tags
                    var tags = item.Elements("category")
                        .Where(c => c.Attribute("domain")?.Value == "post_tag")
                        .Select(c => c.Value)
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToArray();

                    result.TagsCreated += tags.Length;

                    var post = new Post
                    {
                        SiteId = siteId,
                        Title = title,
                        Slug = slug,
                        BodyMarkdown = bodyContent,
                        BodyHtml = bodyContent, // WP content is already HTML
                        AuthorId = importedBy,
                        Status = mappedStatus,
                        Tags = tags.Length > 0 ? tags : null,
                        CategoryId = categoryId
                    };

                    if (DateTime.TryParse(pubDate, out var publishedDate))
                        post.PublishedAt = publishedDate.ToUniversalTime();

                    var createdPost = await _postService.CreateAsync(post);
                    result.PostsImported++;

                    // Import comments for this post
                    foreach (var commentEl in item.Elements(wp + "comment"))
                    {
                        try
                        {
                            var commentStatus = commentEl.Element(wp + "comment_approved")?.Value;
                            if (commentStatus == "spam" || commentStatus == "trash") continue;

                            var comment = new Comment
                            {
                                PostId = createdPost.Id,
                                AuthorName = commentEl.Element(wp + "comment_author")?.Value ?? "Anonymous",
                                AuthorEmail = commentEl.Element(wp + "comment_author_email")?.Value,
                                AuthorUrl = commentEl.Element(wp + "comment_author_url")?.Value,
                                BodyMarkdown = commentEl.Element(wp + "comment_content")?.Value ?? "",
                                Status = commentStatus == "1" ? "approved" : "pending"
                            };

                            await _commentService.CreateAsync(comment);
                            result.CommentsImported++;
                        }
                        catch (Exception ex)
                        {
                            result.Errors++;
                            result.ErrorMessages.Add($"Comment import error: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    result.ErrorMessages.Add($"Post import error: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors++;
            result.ErrorMessages.Add($"WXR parse error: {ex.Message}");
        }

        sw.Stop();
        result.Duration = sw.Elapsed;
        return result;
    }

    public async Task<ImportResult> ImportMarkdownAsync(Guid siteId,
        IEnumerable<(string Filename, string Content)> markdownFiles, Guid importedBy)
    {
        Guard.Against.DefaultStruct(siteId);
        Guard.Against.Null(markdownFiles);
        Guard.Against.DefaultStruct(importedBy);

        var sw = Stopwatch.StartNew();
        var result = new ImportResult();

        foreach (var (filename, content) in markdownFiles)
        {
            try
            {
                var (frontMatter, body) = ParseFrontMatter(content);
                var title = frontMatter.GetValueOrDefault("title") ?? Path.GetFileNameWithoutExtension(filename);
                var slug = frontMatter.GetValueOrDefault("slug") ?? GenerateSlug(title);

                var post = new Post
                {
                    SiteId = siteId,
                    Title = title,
                    Slug = slug,
                    BodyMarkdown = body,
                    AuthorId = importedBy,
                    Status = frontMatter.GetValueOrDefault("status") ?? "draft"
                };

                if (frontMatter.TryGetValue("date", out var dateStr) && DateTime.TryParse(dateStr, out var date))
                    post.PublishedAt = date.ToUniversalTime();

                if (frontMatter.TryGetValue("tags", out var tagsStr))
                    post.Tags = tagsStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (frontMatter.TryGetValue("excerpt", out var excerpt))
                    post.Excerpt = excerpt;

                await _postService.CreateAsync(post);
                result.PostsImported++;
            }
            catch (Exception ex)
            {
                result.Errors++;
                result.ErrorMessages.Add($"Markdown import error ({filename}): {ex.Message}");
            }
        }

        sw.Stop();
        result.Duration = sw.Elapsed;
        return result;
    }

    public async Task<string> ExportSiteJsonAsync(Guid siteId)
    {
        Guard.Against.DefaultStruct(siteId);

        var site = await _siteService.GetByIdAsync(siteId);
        var posts = await _postService.GetAllAsync(siteId, page: 1, pageSize: 10000);
        var categories = await _categoryService.GetAllBySiteAsync(siteId, page: 1, pageSize: 1000);

        var export = new
        {
            version = "1.0.0",
            exportedAt = DateTime.UtcNow,
            site,
            posts = posts.Select(p => new
            {
                p.Id, p.Title, p.Slug, p.Subtitle, p.Excerpt,
                p.BodyMarkdown, p.Status, p.Visibility, p.Tags,
                p.CoverImageUrl, p.PublishedAt, p.CreatedAt,
                p.MetaTitle, p.MetaDescription, p.CanonicalUrl,
                p.Featured, p.WordCount, p.ReadingTimeMinutes
            }),
            categories = categories.Select(c => new
            {
                c.Id, c.Name, c.Slug, c.Description, c.ParentId, c.SortOrder
            })
        };

        return JsonSerializer.Serialize(export, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public async Task<string> ExportPostMarkdownAsync(Guid postId)
    {
        Guard.Against.DefaultStruct(postId);

        var post = await _postService.GetByIdAsync(postId)
            ?? throw new KeyNotFoundException($"Post {postId} not found.");

        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"title: \"{EscapeYaml(post.Title)}\"");
        sb.AppendLine($"slug: \"{post.Slug}\"");
        sb.AppendLine($"status: \"{post.Status}\"");
        if (post.PublishedAt.HasValue)
            sb.AppendLine($"date: \"{post.PublishedAt.Value:yyyy-MM-ddTHH:mm:ssZ}\"");
        if (!string.IsNullOrEmpty(post.Subtitle))
            sb.AppendLine($"subtitle: \"{EscapeYaml(post.Subtitle)}\"");
        if (!string.IsNullOrEmpty(post.Excerpt))
            sb.AppendLine($"excerpt: \"{EscapeYaml(post.Excerpt)}\"");
        if (post.Tags is { Length: > 0 })
            sb.AppendLine($"tags: \"{string.Join(", ", post.Tags)}\"");
        if (!string.IsNullOrEmpty(post.CoverImageUrl))
            sb.AppendLine($"cover_image: \"{post.CoverImageUrl}\"");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.Append(post.BodyMarkdown);

        return sb.ToString();
    }

    public async Task<string> ExportPostHtmlAsync(Guid postId)
    {
        Guard.Against.DefaultStruct(postId);

        var post = await _postService.GetByIdAsync(postId)
            ?? throw new KeyNotFoundException($"Post {postId} not found.");

        var bodyHtml = !string.IsNullOrEmpty(post.BodyHtml)
            ? post.BodyHtml
            : _markdownService.RenderToHtml(post.BodyMarkdown);

        Func<string?, string?> encode = System.Net.WebUtility.HtmlEncode;
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\" />");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
        sb.AppendLine($"  <title>{encode(post.Title)}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body { font-family: Georgia, 'Times New Roman', serif; max-width: 720px; margin: 2rem auto; padding: 0 1rem; color: #1A1A1A; }");
        sb.AppendLine("    h1 { font-size: 2.5rem; margin-bottom: 0.5rem; }");
        sb.AppendLine("    .subtitle { color: #6B6B6B; font-size: 1.25rem; margin-bottom: 2rem; }");
        sb.AppendLine("    .meta { color: #999; font-size: 0.875rem; margin-bottom: 2rem; }");
        sb.AppendLine("    img { max-width: 100%; height: auto; }");
        sb.AppendLine("    pre { background: #f5f5f5; padding: 1rem; overflow-x: auto; border-radius: 4px; }");
        sb.AppendLine("    code { font-family: 'Fira Code', monospace; }");
        sb.AppendLine("    blockquote { border-left: 3px solid #3D5A80; padding-left: 1rem; color: #6B6B6B; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <article>");
        sb.AppendLine($"    <h1>{encode(post.Title)}</h1>");

        if (!string.IsNullOrEmpty(post.Subtitle))
            sb.AppendLine($"    <p class=\"subtitle\">{encode(post.Subtitle)}</p>");

        sb.Append("    <div class=\"meta\">");
        sb.Append(post.PublishedAt.HasValue
            ? $"Published {post.PublishedAt.Value:MMMM d, yyyy}"
            : "Draft");
        if (post.ReadingTimeMinutes.HasValue)
            sb.Append($" &middot; {post.ReadingTimeMinutes} min read");
        sb.AppendLine("</div>");

        if (!string.IsNullOrEmpty(post.CoverImageUrl))
            sb.AppendLine($"    <img src=\"{encode(post.CoverImageUrl)}\" alt=\"{encode(post.Title)}\" />");

        sb.AppendLine("    <div class=\"content\">");
        sb.AppendLine($"      {bodyHtml}");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </article>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static (Dictionary<string, string> FrontMatter, string Body) ParseFrontMatter(string content)
    {
        var frontMatter = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!content.StartsWith("---"))
            return (frontMatter, content);

        var endIndex = content.IndexOf("---", 3, StringComparison.Ordinal);
        if (endIndex < 0)
            return (frontMatter, content);

        var yamlBlock = content[3..endIndex].Trim();
        var body = content[(endIndex + 3)..].TrimStart('\r', '\n');

        foreach (var line in yamlBlock.Split('\n'))
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex <= 0) continue;

            var key = line[..colonIndex].Trim();
            var value = line[(colonIndex + 1)..].Trim().Trim('"', '\'');
            frontMatter[key] = value;
        }

        return (frontMatter, body);
    }

    private static string GenerateSlug(string text)
    {
        return text.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("--", "-")
            .Trim('-');
    }

    private static string EscapeYaml(string text)
    {
        return text.Replace("\"", "\\\"").Replace("\n", " ");
    }
}
