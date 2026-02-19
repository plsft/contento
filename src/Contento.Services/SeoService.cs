using System.Data;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// SEO analysis and sitemap generation service. Analyzes posts for keyword
/// optimization, readability, and meta tag quality, and generates XML sitemaps.
/// </summary>
public class SeoService : ISeoService
{
    private readonly IDbConnection _db;
    private readonly IPostService _postService;
    private readonly ILogger<SeoService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SeoService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="postService">The post service.</param>
    /// <param name="logger">The logger.</param>
    public SeoService(IDbConnection db, IPostService postService, ILogger<SeoService> logger)
    {
        _db = Guard.Against.Null(db);
        _postService = Guard.Against.Null(postService);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task<SeoAnalysis> AnalyzePostAsync(Guid postId, string? focusKeyword = null)
    {
        Guard.Against.Default(postId);

        var post = await _db.GetAsync<Post>(postId);
        if (post == null)
        {
            return new SeoAnalysis
            {
                PostId = postId,
                OverallScore = 0,
                Issues = "[]",
                LastAnalyzedAt = DateTime.UtcNow
            };
        }

        var issues = new List<SeoIssue>();
        var totalScore = 0;
        var body = post.BodyMarkdown ?? string.Empty;
        var keyword = focusKeyword?.Trim() ?? string.Empty;
        var hasKeyword = !string.IsNullOrWhiteSpace(keyword);
        decimal keywordDensity = 0;

        // --- Focus keyword in title (10 pts) ---
        if (hasKeyword)
        {
            if (post.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                totalScore += 10;
                issues.Add(new SeoIssue("Focus keyword in title", "info", "Focus keyword found in title."));
            }
            else
            {
                issues.Add(new SeoIssue("Focus keyword in title", "error", "Focus keyword not found in title."));
            }
        }

        // --- Focus keyword in slug (10 pts) ---
        if (hasKeyword)
        {
            var slugKeyword = keyword.ToLowerInvariant().Replace(' ', '-');
            if ((post.Slug ?? string.Empty).Contains(slugKeyword, StringComparison.OrdinalIgnoreCase))
            {
                totalScore += 10;
                issues.Add(new SeoIssue("Focus keyword in slug", "info", "Focus keyword found in slug."));
            }
            else
            {
                issues.Add(new SeoIssue("Focus keyword in slug", "error", "Focus keyword not found in slug."));
            }
        }

        // --- Focus keyword in excerpt (10 pts) ---
        if (hasKeyword)
        {
            if (!string.IsNullOrWhiteSpace(post.Excerpt) &&
                post.Excerpt.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                totalScore += 10;
                issues.Add(new SeoIssue("Focus keyword in excerpt", "info", "Focus keyword found in excerpt."));
            }
            else
            {
                issues.Add(new SeoIssue("Focus keyword in excerpt", "error", "Focus keyword not found in excerpt."));
            }
        }

        // --- Focus keyword in first paragraph (5 pts) ---
        if (hasKeyword)
        {
            var firstParagraph = body.Length > 200 ? body[..200] : body;
            if (firstParagraph.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                totalScore += 5;
                issues.Add(new SeoIssue("Focus keyword in first paragraph", "info", "Focus keyword found in first paragraph."));
            }
            else
            {
                issues.Add(new SeoIssue("Focus keyword in first paragraph", "error", "Focus keyword not found in first paragraph."));
            }
        }

        // --- Keyword density (10 pts) ---
        if (hasKeyword)
        {
            var words = body.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var wordCount = words.Length;
            if (wordCount > 0)
            {
                var keywordCount = Regex.Matches(body, Regex.Escape(keyword), RegexOptions.IgnoreCase).Count;
                keywordDensity = (decimal)keywordCount / wordCount * 100;

                if (keywordDensity >= 1 && keywordDensity <= 3)
                {
                    totalScore += 10;
                    issues.Add(new SeoIssue("Keyword density", "info", $"Keyword density is {keywordDensity:F1}% (ideal: 1-3%)."));
                }
                else if ((keywordDensity >= 0.5m && keywordDensity < 1) || (keywordDensity > 3 && keywordDensity <= 4))
                {
                    totalScore += 5;
                    issues.Add(new SeoIssue("Keyword density", "warning", $"Keyword density is {keywordDensity:F1}% (ideal: 1-3%)."));
                }
                else
                {
                    issues.Add(new SeoIssue("Keyword density", "error", $"Keyword density is {keywordDensity:F1}% (ideal: 1-3%)."));
                }
            }
        }

        // --- Meta title length (10 pts) ---
        var metaTitle = post.MetaTitle ?? post.Title;
        var metaTitleLen = metaTitle.Length;
        if (metaTitleLen >= 50 && metaTitleLen <= 60)
        {
            totalScore += 10;
            issues.Add(new SeoIssue("Meta title length", "info", $"Meta title is {metaTitleLen} characters (ideal: 50-60)."));
        }
        else if (metaTitleLen >= 40 && metaTitleLen <= 70)
        {
            totalScore += 5;
            issues.Add(new SeoIssue("Meta title length", "warning", $"Meta title is {metaTitleLen} characters (ideal: 50-60)."));
        }
        else
        {
            issues.Add(new SeoIssue("Meta title length", "error", $"Meta title is {metaTitleLen} characters (ideal: 50-60)."));
        }

        // --- Meta description length (10 pts) ---
        var metaDesc = post.MetaDescription ?? string.Empty;
        var metaDescLen = metaDesc.Length;
        if (metaDescLen >= 150 && metaDescLen <= 160)
        {
            totalScore += 10;
            issues.Add(new SeoIssue("Meta description length", "info", $"Meta description is {metaDescLen} characters (ideal: 150-160)."));
        }
        else if (metaDescLen >= 120 && metaDescLen <= 170)
        {
            totalScore += 5;
            issues.Add(new SeoIssue("Meta description length", "warning", $"Meta description is {metaDescLen} characters (ideal: 150-160)."));
        }
        else
        {
            issues.Add(new SeoIssue("Meta description length", "error", $"Meta description is {metaDescLen} characters (ideal: 150-160)."));
        }

        // --- Title length (5 pts) ---
        if (post.Title.Length <= 70)
        {
            totalScore += 5;
            issues.Add(new SeoIssue("Title length", "info", $"Title is {post.Title.Length} characters (max: 70)."));
        }
        else
        {
            issues.Add(new SeoIssue("Title length", "error", $"Title is {post.Title.Length} characters (max: 70)."));
        }

        // --- Content length (10 pts) ---
        var contentWords = body.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        if (contentWords > 600)
        {
            totalScore += 10;
            issues.Add(new SeoIssue("Content length", "info", $"Content has {contentWords} words (good: 600+)."));
        }
        else if (contentWords > 300)
        {
            totalScore += 5;
            issues.Add(new SeoIssue("Content length", "warning", $"Content has {contentWords} words (good: 600+)."));
        }
        else
        {
            issues.Add(new SeoIssue("Content length", "error", $"Content has {contentWords} words (good: 600+)."));
        }

        // --- Image alt text (5 pts) ---
        var hasImages = body.Contains("![");
        if (hasImages)
        {
            var missingAlt = body.Contains("![](");
            if (!missingAlt)
            {
                totalScore += 5;
                issues.Add(new SeoIssue("Image alt text", "info", "All images have alt text."));
            }
            else
            {
                issues.Add(new SeoIssue("Image alt text", "error", "Some images are missing alt text."));
            }
        }
        else
        {
            totalScore += 5;
            issues.Add(new SeoIssue("Image alt text", "info", "No images found (no alt text needed)."));
        }

        // --- Internal links (5 pts) ---
        if (body.Contains("](/"))
        {
            totalScore += 5;
            issues.Add(new SeoIssue("Internal links", "info", "Internal links found."));
        }
        else
        {
            issues.Add(new SeoIssue("Internal links", "error", "No internal links found."));
        }

        // --- External links (5 pts) ---
        if (body.Contains("](http"))
        {
            totalScore += 5;
            issues.Add(new SeoIssue("External links", "info", "External links found."));
        }
        else
        {
            issues.Add(new SeoIssue("External links", "error", "No external links found."));
        }

        // --- Readability (5 pts) ---
        var sentences = Regex.Split(body, @"[.?!]+").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        int readabilityScore = 0;
        if (sentences.Length > 0)
        {
            var avgSentenceLength = sentences
                .Average(s => s.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length);

            if (avgSentenceLength < 20)
            {
                totalScore += 5;
                readabilityScore = 5;
                issues.Add(new SeoIssue("Readability", "info", $"Average sentence length is {avgSentenceLength:F1} words (good: under 20)."));
            }
            else
            {
                issues.Add(new SeoIssue("Readability", "error", $"Average sentence length is {avgSentenceLength:F1} words (good: under 20)."));
            }
        }
        else
        {
            totalScore += 5;
            readabilityScore = 5;
        }

        // Build analysis
        var analysis = new SeoAnalysis
        {
            PostId = postId,
            OverallScore = totalScore,
            Issues = JsonSerializer.Serialize(issues),
            FocusKeyword = hasKeyword ? keyword : null,
            KeywordDensity = hasKeyword ? keywordDensity : null,
            ReadabilityScore = readabilityScore,
            LastAnalyzedAt = DateTime.UtcNow
        };

        // Upsert: check if analysis exists for this post
        var existing = await _db.QueryAsync<SeoAnalysis>(
            "SELECT * FROM seo_analyses WHERE post_id = @PostId LIMIT 1",
            new { PostId = postId });

        var existingAnalysis = existing.FirstOrDefault();
        if (existingAnalysis != null)
        {
            analysis.Id = existingAnalysis.Id;
            await _db.UpdateAsync(analysis);
        }
        else
        {
            analysis.Id = Guid.NewGuid();
            await _db.InsertAsync(analysis);
        }

        return analysis;
    }

    /// <inheritdoc />
    public async Task<SeoAnalysis?> GetAnalysisAsync(Guid postId)
    {
        Guard.Against.Default(postId);

        var results = await _db.QueryAsync<SeoAnalysis>(
            "SELECT * FROM seo_analyses WHERE post_id = @PostId LIMIT 1",
            new { PostId = postId });

        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public Task<string> GenerateSitemapIndexAsync(Guid siteId, string baseUrl)
    {
        Guard.Against.Default(siteId);
        Guard.Against.NullOrWhiteSpace(baseUrl);

        var escapedBaseUrl = System.Security.SecurityElement.Escape(baseUrl.TrimEnd('/'));

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<sitemapindex xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        sb.AppendLine($"  <sitemap><loc>{escapedBaseUrl}/sitemap-posts-1.xml</loc></sitemap>");
        sb.AppendLine($"  <sitemap><loc>{escapedBaseUrl}/sitemap-categories.xml</loc></sitemap>");
        sb.AppendLine($"  <sitemap><loc>{escapedBaseUrl}/sitemap-tags.xml</loc></sitemap>");
        sb.AppendLine($"  <sitemap><loc>{escapedBaseUrl}/sitemap-pages.xml</loc></sitemap>");
        sb.AppendLine("</sitemapindex>");

        return Task.FromResult(sb.ToString());
    }

    /// <inheritdoc />
    public async Task<string> GeneratePostSitemapAsync(Guid siteId, string baseUrl, int page = 1)
    {
        Guard.Against.Default(siteId);
        Guard.Against.NullOrWhiteSpace(baseUrl);

        var offset = (Math.Max(page, 1) - 1) * 1000;
        var escapedBaseUrl = System.Security.SecurityElement.Escape(baseUrl.TrimEnd('/'));

        var posts = await _db.QueryAsync<Post>(
            "SELECT * FROM posts WHERE site_id = @SiteId AND status = 'published' ORDER BY published_at DESC LIMIT 1000 OFFSET @Offset",
            new { SiteId = siteId, Offset = offset });

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:image=\"http://www.google.com/schemas/sitemap-image/1.1\">");

        foreach (var post in posts)
        {
            var escapedSlug = System.Security.SecurityElement.Escape(post.Slug);
            var lastmod = post.UpdatedAt.ToString("yyyy-MM-dd");

            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{escapedBaseUrl}/{escapedSlug}</loc>");
            sb.AppendLine($"    <lastmod>{lastmod}</lastmod>");
            sb.AppendLine("    <priority>0.8</priority>");

            if (!string.IsNullOrWhiteSpace(post.CoverImageUrl))
            {
                var escapedImageUrl = System.Security.SecurityElement.Escape(post.CoverImageUrl);
                sb.AppendLine($"    <image:image><image:loc>{escapedImageUrl}</image:loc></image:image>");
            }

            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");
        return sb.ToString();
    }

    /// <inheritdoc />
    public async Task<string> GenerateCategorySitemapAsync(Guid siteId, string baseUrl)
    {
        Guard.Against.Default(siteId);
        Guard.Against.NullOrWhiteSpace(baseUrl);

        var escapedBaseUrl = System.Security.SecurityElement.Escape(baseUrl.TrimEnd('/'));

        var categories = await _db.QueryAsync<Category>(
            "SELECT * FROM categories WHERE site_id = @SiteId ORDER BY name",
            new { SiteId = siteId });

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        foreach (var category in categories)
        {
            var escapedSlug = System.Security.SecurityElement.Escape(category.Slug);
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{escapedBaseUrl}/category/{escapedSlug}</loc>");
            sb.AppendLine("    <priority>0.6</priority>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");
        return sb.ToString();
    }

    /// <inheritdoc />
    public async Task<string> GenerateTagSitemapAsync(Guid siteId, string baseUrl)
    {
        Guard.Against.Default(siteId);
        Guard.Against.NullOrWhiteSpace(baseUrl);

        var escapedBaseUrl = System.Security.SecurityElement.Escape(baseUrl.TrimEnd('/'));

        var tags = await _db.QueryAsync<string>(
            "SELECT DISTINCT unnest(tags) as tag FROM posts WHERE site_id = @SiteId AND status = 'published'",
            new { SiteId = siteId });

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        foreach (var tag in tags)
        {
            var escapedTag = System.Security.SecurityElement.Escape(tag);
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{escapedBaseUrl}/tag/{escapedTag}</loc>");
            sb.AppendLine("    <priority>0.5</priority>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");
        return sb.ToString();
    }

    /// <inheritdoc />
    public async Task<string> GeneratePageSitemapAsync(Guid siteId, string baseUrl)
    {
        Guard.Against.Default(siteId);
        Guard.Against.NullOrWhiteSpace(baseUrl);

        var escapedBaseUrl = System.Security.SecurityElement.Escape(baseUrl.TrimEnd('/'));

        var pages = await _db.QueryAsync<Post>(
            "SELECT * FROM posts WHERE site_id = @SiteId AND status = 'published' ORDER BY published_at DESC",
            new { SiteId = siteId });

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        foreach (var page in pages)
        {
            var escapedSlug = System.Security.SecurityElement.Escape(page.Slug);
            var lastmod = page.UpdatedAt.ToString("yyyy-MM-dd");

            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{escapedBaseUrl}/{escapedSlug}</loc>");
            sb.AppendLine($"    <lastmod>{lastmod}</lastmod>");
            sb.AppendLine("    <priority>0.6</priority>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");
        return sb.ToString();
    }

    /// <summary>
    /// Helper record for serializing SEO issues to JSON.
    /// </summary>
    private record SeoIssue(string Check, string Severity, string Message);
}
