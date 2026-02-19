using System.Data;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for full-text search of published posts using PostgreSQL tsvector
/// and trigram similarity matching.
/// </summary>
public class SearchService : ISearchService
{
    private readonly IDbConnection _db;
    private readonly ILogger<SearchService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SearchService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="logger">The logger.</param>
    public SearchService(IDbConnection db, ILogger<SearchService> logger)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Post>> SearchPostsAsync(Guid siteId, string query,
        int page = 1, int pageSize = 20)
    {
        Guard.Against.Default(siteId);
        Guard.Against.NullOrWhiteSpace(query);

        var offset = (Math.Max(page, 1) - 1) * pageSize;

        return await _db.QueryAsync<Post>(
            @"SELECT *,
                     ts_rank(
                         to_tsvector('english', coalesce(title, '') || ' ' || coalesce(body_markdown, '')),
                         plainto_tsquery('english', @Query)
                     ) AS rank
              FROM posts
              WHERE site_id = @SiteId
                AND status = 'published'
                AND to_tsvector('english', coalesce(title, '') || ' ' || coalesce(body_markdown, ''))
                    @@ plainto_tsquery('english', @Query)
              ORDER BY rank DESC
              LIMIT @Limit OFFSET @Offset",
            new { SiteId = siteId, Query = query, Limit = pageSize, Offset = offset });
    }

    /// <inheritdoc />
    public async Task<int> GetSearchResultCountAsync(Guid siteId, string query)
    {
        Guard.Against.Default(siteId);
        Guard.Against.NullOrWhiteSpace(query);

        return await _db.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*)
              FROM posts
              WHERE site_id = @SiteId
                AND status = 'published'
                AND to_tsvector('english', coalesce(title, '') || ' ' || coalesce(body_markdown, ''))
                    @@ plainto_tsquery('english', @Query)",
            new { SiteId = siteId, Query = query });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetSuggestionsAsync(Guid siteId, string partialQuery, int limit = 5)
    {
        Guard.Against.Default(siteId);
        Guard.Against.NullOrWhiteSpace(partialQuery);

        return await _db.QueryAsync<string>(
            @"SELECT title
              FROM posts
              WHERE site_id = @SiteId
                AND status = 'published'
                AND (title ILIKE @Pattern
                     OR similarity(title, @Query) > 0.1)
              ORDER BY similarity(title, @Query) DESC
              LIMIT @Limit",
            new { SiteId = siteId, Pattern = $"%{partialQuery}%", Query = partialQuery, Limit = limit });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<(Post Post, string HighlightedTitle, string HighlightedExcerpt)>>
        SearchWithHighlightsAsync(Guid siteId, string query, int page = 1, int pageSize = 20)
    {
        Guard.Against.Default(siteId);
        Guard.Against.NullOrWhiteSpace(query);

        var offset = (Math.Max(page, 1) - 1) * pageSize;

        var rows = await _db.QueryAsync<dynamic>(
            @"SELECT *,
                     ts_headline('english', title, plainto_tsquery('english', @Query),
                         'StartSel=<mark>,StopSel=</mark>,MaxWords=50,MinWords=20') AS highlighted_title,
                     ts_headline('english', coalesce(body_markdown, ''), plainto_tsquery('english', @Query),
                         'StartSel=<mark>,StopSel=</mark>,MaxWords=80,MinWords=30') AS highlighted_excerpt,
                     ts_rank(
                         to_tsvector('english', coalesce(title, '') || ' ' || coalesce(body_markdown, '')),
                         plainto_tsquery('english', @Query)
                     ) AS rank
              FROM posts
              WHERE site_id = @SiteId
                AND status = 'published'
                AND to_tsvector('english', coalesce(title, '') || ' ' || coalesce(body_markdown, ''))
                    @@ plainto_tsquery('english', @Query)
              ORDER BY rank DESC
              LIMIT @Limit OFFSET @Offset",
            new { SiteId = siteId, Query = query, Limit = pageSize, Offset = offset });

        var results = new List<(Post, string, string)>();
        foreach (var row in rows)
        {
            var post = new Post
            {
                Id = row.id,
                SiteId = row.site_id,
                Title = row.title,
                Slug = row.slug,
                Excerpt = row.excerpt,
                BodyMarkdown = row.body_markdown ?? string.Empty,
                BodyHtml = row.body_html,
                Status = row.status,
                PublishedAt = row.published_at,
                AuthorId = row.author_id
            };
            results.Add((post, (string)row.highlighted_title, (string)row.highlighted_excerpt));
        }

        return results;
    }
}
