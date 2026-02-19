using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Full CRUD service for blog posts with publishing workflow, versioning,
/// slug generation, and filtered listing.
/// </summary>
public class PostService : IPostService
{
    private readonly IDbConnection _db;
    private readonly IMarkdownService _markdown;
    private readonly ILogger<PostService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PostService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="markdown">The Markdown rendering service.</param>
    /// <param name="logger">The logger.</param>
    public PostService(IDbConnection db, IMarkdownService markdown, ILogger<PostService> logger)
    {
        _db = Guard.Against.Null(db);
        _markdown = Guard.Against.Null(markdown);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task<Post?> GetByIdAsync(Guid id)
    {
        Guard.Against.Default(id);
        return await _db.GetAsync<Post>(id);
    }

    /// <inheritdoc />
    public async Task<Post?> GetBySlugAsync(Guid siteId, string slug)
    {
        Guard.Against.Default(siteId);
        Guard.Against.NullOrWhiteSpace(slug);

        var results = await _db.QueryAsync<Post>(
            "SELECT * FROM posts WHERE site_id = @SiteId AND slug = @Slug LIMIT 1",
            new { SiteId = siteId, Slug = slug });
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<Post> CreateAsync(Post post)
    {
        Guard.Against.Null(post);
        Guard.Against.NullOrWhiteSpace(post.Title);
        Guard.Against.Default(post.SiteId);
        Guard.Against.Default(post.AuthorId);

        post.Id = Guid.NewGuid();
        post.Slug = await GenerateUniqueSlugAsync(post.SiteId, post.Title, null);
        post.BodyHtml = _markdown.RenderToHtml(post.BodyMarkdown);
        post.WordCount = _markdown.CalculateWordCount(post.BodyMarkdown);
        post.ReadingTimeMinutes = _markdown.CalculateReadingTime(post.BodyMarkdown);
        post.Version = 1;
        post.CreatedAt = DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;

        await _db.InsertAsync(post);
        return post;
    }

    /// <inheritdoc />
    public async Task<Post> UpdateAsync(Post post, Guid changedBy, string? changeSummary = null)
    {
        Guard.Against.Null(post);
        Guard.Against.Default(post.Id);
        Guard.Against.Default(changedBy);

        // Create a version snapshot before applying changes
        var version = new PostVersion
        {
            Id = Guid.NewGuid(),
            PostId = post.Id,
            Version = post.Version,
            Title = post.Title,
            BodyMarkdown = post.BodyMarkdown,
            BodyHtml = post.BodyHtml,
            ChangeSummary = changeSummary,
            ChangedBy = changedBy,
            CreatedAt = DateTime.UtcNow
        };
        await _db.InsertAsync(version);

        // Recalculate derived fields
        post.BodyHtml = _markdown.RenderToHtml(post.BodyMarkdown);
        post.WordCount = _markdown.CalculateWordCount(post.BodyMarkdown);
        post.ReadingTimeMinutes = _markdown.CalculateReadingTime(post.BodyMarkdown);
        post.Version += 1;
        post.UpdatedAt = DateTime.UtcNow;

        await _db.UpdateAsync(post);
        return post;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        Guard.Against.Default(id);

        await _db.ExecuteAsync(
            "UPDATE posts SET status = @Status, updated_at = @Now WHERE id = @Id",
            new { Status = "archived", Now = DateTime.UtcNow, Id = id });
    }

    /// <inheritdoc />
    public async Task PublishAsync(Guid id)
    {
        Guard.Against.Default(id);

        var now = DateTime.UtcNow;
        await _db.ExecuteAsync(
            "UPDATE posts SET status = @Status, published_at = @PublishedAt, updated_at = @Now WHERE id = @Id",
            new { Status = "published", PublishedAt = now, Now = now, Id = id });
    }

    /// <inheritdoc />
    public async Task UnpublishAsync(Guid id)
    {
        Guard.Against.Default(id);

        await _db.ExecuteAsync(
            "UPDATE posts SET status = @Status, published_at = NULL, updated_at = @Now WHERE id = @Id",
            new { Status = "draft", Now = DateTime.UtcNow, Id = id });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Post>> GetAllAsync(Guid siteId, string? status = null,
        Guid? categoryId = null, string? tag = null, string? search = null,
        int page = 1, int pageSize = 20)
    {
        Guard.Against.Default(siteId);

        var (whereClause, parameters) = BuildFilterClause(siteId, status, categoryId, tag, search);
        var offset = (Math.Max(page, 1) - 1) * pageSize;

        var sql = $@"SELECT * FROM posts
                     WHERE {whereClause}
                     ORDER BY created_at DESC
                     LIMIT @Limit OFFSET @Offset";

        parameters.Add("Limit", pageSize);
        parameters.Add("Offset", offset);

        return await _db.QueryAsync<Post>(sql, parameters);
    }

    /// <inheritdoc />
    public async Task<int> GetTotalCountAsync(Guid siteId, string? status = null,
        Guid? categoryId = null, string? tag = null, string? search = null)
    {
        Guard.Against.Default(siteId);

        var (whereClause, parameters) = BuildFilterClause(siteId, status, categoryId, tag, search);
        var sql = $"SELECT COUNT(*) FROM posts WHERE {whereClause}";

        return await _db.ExecuteScalarAsync<int>(sql, parameters);
    }

    /// <summary>
    /// Generates a URL-friendly slug from the title, with collision handling by
    /// appending -2, -3, etc. if the slug already exists for the site.
    /// </summary>
    private async Task<string> GenerateUniqueSlugAsync(Guid siteId, string title, Guid? excludePostId)
    {
        var baseSlug = Slugify(title);

        var sql = excludePostId.HasValue
            ? "SELECT slug FROM posts WHERE site_id = @SiteId AND slug LIKE @Pattern AND id != @ExcludeId"
            : "SELECT slug FROM posts WHERE site_id = @SiteId AND slug LIKE @Pattern";

        var existingSlugs = await _db.QueryAsync<string>(sql, new
        {
            SiteId = siteId,
            Pattern = baseSlug + "%",
            ExcludeId = excludePostId ?? Guid.Empty
        });

        var slugSet = new HashSet<string>(existingSlugs, StringComparer.OrdinalIgnoreCase);
        if (!slugSet.Contains(baseSlug))
            return baseSlug;

        var counter = 2;
        while (slugSet.Contains($"{baseSlug}-{counter}"))
            counter++;

        return $"{baseSlug}-{counter}";
    }

    /// <summary>
    /// Converts a title string into a URL-friendly slug.
    /// </summary>
    private static string Slugify(string text)
    {
        var slug = text.ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"[\s-]+", "-");
        slug = slug.Trim('-');
        return string.IsNullOrEmpty(slug) ? "post" : slug;
    }

    /// <summary>
    /// Builds the WHERE clause and parameter dictionary for filtered post queries.
    /// </summary>
    private static (string WhereClause, Dictionary<string, object> Parameters) BuildFilterClause(
        Guid siteId, string? status, Guid? categoryId, string? tag, string? search)
    {
        var conditions = new List<string> { "site_id = @SiteId" };
        var parameters = new Dictionary<string, object> { { "SiteId", siteId } };

        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("status = @Status");
            parameters["Status"] = status;
        }

        if (categoryId.HasValue && categoryId.Value != Guid.Empty)
        {
            conditions.Add("category_id = @CategoryId");
            parameters["CategoryId"] = categoryId.Value;
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            conditions.Add("@Tag = ANY(tags)");
            parameters["Tag"] = tag;
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            conditions.Add(
                "to_tsvector('english', coalesce(title, '') || ' ' || coalesce(body_markdown, '')) @@ plainto_tsquery('english', @Search)");
            parameters["Search"] = search;
        }

        return (string.Join(" AND ", conditions), parameters);
    }
}
