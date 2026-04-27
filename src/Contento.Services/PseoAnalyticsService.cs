using System.Data;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Google Search Console analytics integration — OAuth flow, data sync, and reporting queries.
/// </summary>
public class PseoAnalyticsService : IPseoAnalyticsService
{
    private readonly IDbConnection _db;
    private readonly IPseoProjectService _projectService;
    private readonly IPseoPageService _pageService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PseoAnalyticsService> _logger;

    private const string GoogleAuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string GoogleTokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string GscApiBase = "https://www.googleapis.com/webmasters/v3/sites";
    private const string GscScope = "https://www.googleapis.com/auth/webmasters.readonly";

    public PseoAnalyticsService(
        IDbConnection db,
        IPseoProjectService projectService,
        IPseoPageService pageService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<PseoAnalyticsService> logger)
    {
        _db = Guard.Against.Null(db);
        _projectService = Guard.Against.Null(projectService);
        _pageService = Guard.Against.Null(pageService);
        _httpClientFactory = Guard.Against.Null(httpClientFactory);
        _configuration = Guard.Against.Null(configuration);
        _logger = Guard.Against.Null(logger);
    }

    // ───────────────────────────────────────────────
    // GSC OAuth
    // ───────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<string> GetGscAuthUrlAsync(Guid projectId, string redirectUri)
    {
        Guard.Against.Default(projectId);
        Guard.Against.NullOrWhiteSpace(redirectUri);

        var project = await _projectService.GetByIdAsync(projectId)
            ?? throw new ArgumentException("Project not found.");

        var clientId = _configuration["GoogleSearchConsole:ClientId"]
            ?? throw new InvalidOperationException("GoogleSearchConsole:ClientId is not configured.");

        var state = Convert.ToBase64String(Encoding.UTF8.GetBytes(projectId.ToString()));

        var url = $"{GoogleAuthEndpoint}?" +
            $"client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_type=code" +
            $"&scope={Uri.EscapeDataString(GscScope)}" +
            $"&access_type=offline" +
            $"&prompt=consent" +
            $"&state={Uri.EscapeDataString(state)}";

        return url;
    }

    /// <inheritdoc />
    public async Task ExchangeGscCodeAsync(Guid projectId, string code, string redirectUri)
    {
        Guard.Against.Default(projectId);
        Guard.Against.NullOrWhiteSpace(code);
        Guard.Against.NullOrWhiteSpace(redirectUri);

        var project = await _projectService.GetByIdAsync(projectId)
            ?? throw new ArgumentException("Project not found.");

        var clientId = _configuration["GoogleSearchConsole:ClientId"]
            ?? throw new InvalidOperationException("GoogleSearchConsole:ClientId is not configured.");
        var clientSecret = _configuration["GoogleSearchConsole:ClientSecret"]
            ?? throw new InvalidOperationException("GoogleSearchConsole:ClientSecret is not configured.");

        using var client = _httpClientFactory.CreateClient();
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        });

        var response = await client.PostAsync(GoogleTokenEndpoint, tokenRequest);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("GSC token exchange failed: {StatusCode} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"GSC token exchange failed: {response.StatusCode}");
        }

        var json = JsonDocument.Parse(body);
        var accessToken = json.RootElement.GetProperty("access_token").GetString()!;
        var refreshToken = json.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();
        var expiry = DateTime.UtcNow.AddSeconds(expiresIn).ToString("o");

        // Store tokens in the project's Settings JSONB
        var settings = JsonNode.Parse(project.Settings ?? "{}") as JsonObject ?? new JsonObject();
        settings["gsc_access_token"] = accessToken;
        if (refreshToken != null)
            settings["gsc_refresh_token"] = refreshToken;
        settings["gsc_token_expiry"] = expiry;
        settings["gsc_site_url"] = $"sc-domain:{project.RootDomain}";

        project.Settings = settings.ToJsonString();
        project.UpdatedAt = DateTime.UtcNow;
        await _projectService.UpdateAsync(project);

        _logger.LogInformation("GSC tokens stored for project {ProjectId}", projectId);
    }

    // ───────────────────────────────────────────────
    // Sync
    // ───────────────────────────────────────────────

    /// <inheritdoc />
    public async Task SyncGscDataAsync(Guid projectId, CancellationToken ct = default)
    {
        Guard.Against.Default(projectId);

        var project = await _projectService.GetByIdAsync(projectId)
            ?? throw new ArgumentException("Project not found.");

        var settings = JsonNode.Parse(project.Settings ?? "{}") as JsonObject;
        var accessToken = settings?["gsc_access_token"]?.GetValue<string>();

        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogWarning("No GSC access token for project {ProjectId}, skipping sync", projectId);
            return;
        }

        // Refresh token if expired
        accessToken = await EnsureValidTokenAsync(project, settings!);

        var siteUrl = settings?["gsc_site_url"]?.GetValue<string>() ?? $"sc-domain:{project.RootDomain}";
        var endDate = DateTime.UtcNow.Date.AddDays(-1); // GSC data has ~2 day delay
        var startDate = endDate.AddDays(-30);

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var requestBody = new
        {
            startDate = startDate.ToString("yyyy-MM-dd"),
            endDate = endDate.ToString("yyyy-MM-dd"),
            dimensions = new[] { "page", "date" },
            rowLimit = 25000
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var encodedSiteUrl = Uri.EscapeDataString(siteUrl);
        var apiUrl = $"{GscApiBase}/{encodedSiteUrl}/searchAnalytics/query";

        var response = await client.PostAsync(apiUrl, content, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("GSC API call failed: {StatusCode} {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"GSC API call failed: {response.StatusCode}");
        }

        var gscData = JsonDocument.Parse(responseBody);

        // Load all project pages for URL matching
        var allPages = await _pageService.GetByProjectIdAsync(projectId, null, 1, 100000);
        var pagesBySlug = allPages.ToDictionary(p => p.Slug.TrimStart('/').ToLowerInvariant(), p => p);

        var upsertCount = 0;

        if (gscData.RootElement.TryGetProperty("rows", out var rows))
        {
            foreach (var row in rows.EnumerateArray())
            {
                ct.ThrowIfCancellationRequested();

                var keys = row.GetProperty("keys");
                var pageUrl = keys[0].GetString() ?? "";
                var dateStr = keys[1].GetString() ?? "";

                if (!DateTime.TryParse(dateStr, out var date))
                    continue;

                var clicks = row.GetProperty("clicks").GetDouble();
                var impressions = row.GetProperty("impressions").GetDouble();
                var ctr = row.GetProperty("ctr").GetDouble();
                var position = row.GetProperty("position").GetDouble();

                // Extract slug from the full URL
                var slug = ExtractSlugFromUrl(pageUrl, project.Fqdn);
                if (string.IsNullOrEmpty(slug))
                    continue;

                if (!pagesBySlug.TryGetValue(slug.TrimStart('/').ToLowerInvariant(), out var matchedPage))
                    continue;

                // Upsert analytics record
                var existing = await _db.QueryAsync<PseoAnalytics>(
                    @"SELECT * FROM pseo_analytics
                      WHERE page_id = @PageId AND date = @Date LIMIT 1",
                    new { PageId = matchedPage.Id, Date = date.Date });

                var record = existing.FirstOrDefault();
                var isIndexed = impressions > 0;

                if (record != null)
                {
                    record.Clicks = (int)clicks;
                    record.Impressions = (int)impressions;
                    record.Ctr = (decimal)ctr;
                    record.Position = (decimal)position;
                    record.IsIndexed = isIndexed;
                    await _db.UpdateAsync(record);
                }
                else
                {
                    record = new PseoAnalytics
                    {
                        Id = Guid.NewGuid(),
                        PageId = matchedPage.Id,
                        Date = date.Date,
                        Clicks = (int)clicks,
                        Impressions = (int)impressions,
                        Ctr = (decimal)ctr,
                        Position = (decimal)position,
                        IsIndexed = isIndexed,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _db.InsertAsync(record);
                }

                upsertCount++;
            }
        }

        // Update last sync timestamp in project settings
        settings!["gsc_last_sync"] = DateTime.UtcNow.ToString("o");
        project.Settings = settings.ToJsonString();
        project.UpdatedAt = DateTime.UtcNow;
        await _projectService.UpdateAsync(project);

        _logger.LogInformation("GSC sync complete for project {ProjectId}: {Count} records upserted", projectId, upsertCount);
    }

    // ───────────────────────────────────────────────
    // Queries
    // ───────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<PseoAnalyticsSummary> GetProjectSummaryAsync(Guid projectId, int days = 30)
    {
        Guard.Against.Default(projectId);

        var since = DateTime.UtcNow.Date.AddDays(-days);

        var summary = new PseoAnalyticsSummary();

        // Total / published page counts
        var pageCounts = await _db.QueryAsync<dynamic>(
            @"SELECT
                COUNT(*) AS total,
                COUNT(*) FILTER (WHERE status = 'published') AS published
              FROM pseo_pages WHERE project_id = @ProjectId",
            new { ProjectId = projectId });

        var counts = pageCounts.FirstOrDefault();
        summary.TotalPages = (int)(counts?.total ?? 0);
        summary.PublishedPages = (int)(counts?.published ?? 0);

        // Aggregate analytics
        var analyticsAgg = await _db.QueryAsync<dynamic>(
            @"SELECT
                COALESCE(SUM(a.clicks), 0) AS total_clicks,
                COALESCE(SUM(a.impressions), 0) AS total_impressions,
                COALESCE(AVG(a.ctr), 0) AS avg_ctr,
                COALESCE(AVG(a.position), 0) AS avg_position,
                COUNT(DISTINCT a.page_id) FILTER (WHERE a.is_indexed) AS indexed_pages
              FROM pseo_analytics a
              JOIN pseo_pages p ON p.id = a.page_id
              WHERE p.project_id = @ProjectId AND a.date >= @Since",
            new { ProjectId = projectId, Since = since });

        var agg = analyticsAgg.FirstOrDefault();
        summary.TotalClicks = (int)(agg?.total_clicks ?? 0);
        summary.TotalImpressions = (int)(agg?.total_impressions ?? 0);
        summary.AvgCtr = (decimal)(agg?.avg_ctr ?? 0m);
        summary.AvgPosition = (decimal)(agg?.avg_position ?? 0m);
        summary.IndexedPages = (int)(agg?.indexed_pages ?? 0);

        // Daily traffic
        var daily = await _db.QueryAsync<DailyTraffic>(
            @"SELECT a.date AS ""Date"",
                     COALESCE(SUM(a.clicks), 0) AS ""Clicks"",
                     COALESCE(SUM(a.impressions), 0) AS ""Impressions""
              FROM pseo_analytics a
              JOIN pseo_pages p ON p.id = a.page_id
              WHERE p.project_id = @ProjectId AND a.date >= @Since
              GROUP BY a.date
              ORDER BY a.date",
            new { ProjectId = projectId, Since = since });

        summary.DailyTraffic = daily.ToList();

        return summary;
    }

    /// <inheritdoc />
    public async Task<List<PseoPageAnalytics>> GetPageAnalyticsAsync(Guid projectId, int days = 30, int page = 1, int pageSize = 50)
    {
        Guard.Against.Default(projectId);

        var since = DateTime.UtcNow.Date.AddDays(-days);
        var offset = (page - 1) * pageSize;

        var results = await _db.QueryAsync<PseoPageAnalytics>(
            @"SELECT p.id AS ""PageId"", p.title AS ""Title"", p.slug AS ""Slug"",
                     COALESCE(SUM(a.clicks), 0) AS ""Clicks"",
                     COALESCE(SUM(a.impressions), 0) AS ""Impressions"",
                     COALESCE(AVG(a.ctr), 0) AS ""Ctr"",
                     COALESCE(AVG(a.position), 0) AS ""Position"",
                     BOOL_OR(COALESCE(a.is_indexed, false)) AS ""IsIndexed""
              FROM pseo_pages p
              LEFT JOIN pseo_analytics a ON a.page_id = p.id AND a.date >= @Since
              WHERE p.project_id = @ProjectId
              GROUP BY p.id, p.title, p.slug
              ORDER BY ""Clicks"" DESC
              LIMIT @PageSize OFFSET @Offset",
            new { ProjectId = projectId, Since = since, PageSize = pageSize, Offset = offset });

        return results.ToList();
    }

    /// <inheritdoc />
    public async Task<List<PseoPageAnalytics>> GetTopPagesAsync(Guid projectId, int days = 30, int limit = 10)
    {
        Guard.Against.Default(projectId);

        var since = DateTime.UtcNow.Date.AddDays(-days);

        var results = await _db.QueryAsync<PseoPageAnalytics>(
            @"SELECT p.id AS ""PageId"", p.title AS ""Title"", p.slug AS ""Slug"",
                     COALESCE(SUM(a.clicks), 0) AS ""Clicks"",
                     COALESCE(SUM(a.impressions), 0) AS ""Impressions"",
                     COALESCE(AVG(a.ctr), 0) AS ""Ctr"",
                     COALESCE(AVG(a.position), 0) AS ""Position"",
                     BOOL_OR(COALESCE(a.is_indexed, false)) AS ""IsIndexed""
              FROM pseo_pages p
              JOIN pseo_analytics a ON a.page_id = p.id AND a.date >= @Since
              WHERE p.project_id = @ProjectId
              GROUP BY p.id, p.title, p.slug
              ORDER BY ""Clicks"" DESC
              LIMIT @Limit",
            new { ProjectId = projectId, Since = since, Limit = limit });

        return results.ToList();
    }

    /// <inheritdoc />
    public async Task<List<NichePerformance>> GetNichePerformanceAsync(Guid projectId, int days = 30)
    {
        Guard.Against.Default(projectId);

        var since = DateTime.UtcNow.Date.AddDays(-days);

        var results = await _db.QueryAsync<NichePerformance>(
            @"SELECT p.niche_slug AS ""NicheSlug"",
                     COUNT(DISTINCT p.id) AS ""PageCount"",
                     COALESCE(SUM(a.clicks), 0) AS ""TotalClicks"",
                     COALESCE(SUM(a.impressions), 0) AS ""TotalImpressions"",
                     COALESCE(AVG(a.position), 0) AS ""AvgPosition""
              FROM pseo_pages p
              LEFT JOIN pseo_analytics a ON a.page_id = p.id AND a.date >= @Since
              WHERE p.project_id = @ProjectId AND p.niche_slug != ''
              GROUP BY p.niche_slug
              ORDER BY ""TotalClicks"" DESC",
            new { ProjectId = projectId, Since = since });

        return results.ToList();
    }

    /// <inheritdoc />
    public async Task<List<PseoPage>> GetZeroTrafficPagesAsync(Guid projectId, int daysSincePublish = 60)
    {
        Guard.Against.Default(projectId);

        var publishedBefore = DateTime.UtcNow.AddDays(-daysSincePublish);

        var results = await _db.QueryAsync<PseoPage>(
            @"SELECT p.* FROM pseo_pages p
              WHERE p.project_id = @ProjectId
                AND p.status = 'published'
                AND p.published_at <= @PublishedBefore
                AND NOT EXISTS (
                    SELECT 1 FROM pseo_analytics a
                    WHERE a.page_id = p.id AND a.clicks > 0
                )
              ORDER BY p.published_at ASC",
            new { ProjectId = projectId, PublishedBefore = publishedBefore });

        return results.ToList();
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportCsvAsync(Guid projectId, int days = 30)
    {
        Guard.Against.Default(projectId);

        var pages = await GetPageAnalyticsAsync(projectId, days, 1, 100000);

        var sb = new StringBuilder();
        sb.AppendLine("Title,Slug,Clicks,Impressions,CTR,Position,Indexed");

        foreach (var p in pages)
        {
            var title = EscapeCsv(p.Title);
            var slug = EscapeCsv(p.Slug);
            sb.AppendLine($"{title},{slug},{p.Clicks},{p.Impressions},{p.Ctr:F4},{p.Position:F1},{p.IsIndexed}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    // ───────────────────────────────────────────────
    // Helpers
    // ───────────────────────────────────────────────

    private async Task<string> EnsureValidTokenAsync(PseoProject project, JsonObject settings)
    {
        var accessToken = settings["gsc_access_token"]?.GetValue<string>();
        var expiryStr = settings["gsc_token_expiry"]?.GetValue<string>();
        var refreshToken = settings["gsc_refresh_token"]?.GetValue<string>();

        if (string.IsNullOrEmpty(accessToken))
            throw new InvalidOperationException("No GSC access token configured.");

        // Check if token is still valid (with 5-minute buffer)
        if (!string.IsNullOrEmpty(expiryStr) && DateTime.TryParse(expiryStr, out var expiry))
        {
            if (expiry > DateTime.UtcNow.AddMinutes(5))
                return accessToken;
        }

        // Token expired — refresh it
        if (string.IsNullOrEmpty(refreshToken))
            throw new InvalidOperationException("GSC refresh token is missing. Please re-authorize.");

        var clientId = _configuration["GoogleSearchConsole:ClientId"]
            ?? throw new InvalidOperationException("GoogleSearchConsole:ClientId is not configured.");
        var clientSecret = _configuration["GoogleSearchConsole:ClientSecret"]
            ?? throw new InvalidOperationException("GoogleSearchConsole:ClientSecret is not configured.");

        using var client = _httpClientFactory.CreateClient();
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["grant_type"] = "refresh_token"
        });

        var response = await client.PostAsync(GoogleTokenEndpoint, tokenRequest);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("GSC token refresh failed: {StatusCode} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"GSC token refresh failed: {response.StatusCode}");
        }

        var json = JsonDocument.Parse(body);
        var newAccessToken = json.RootElement.GetProperty("access_token").GetString()!;
        var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();
        var newExpiry = DateTime.UtcNow.AddSeconds(expiresIn).ToString("o");

        settings["gsc_access_token"] = newAccessToken;
        settings["gsc_token_expiry"] = newExpiry;

        // If a new refresh token is returned, store it
        if (json.RootElement.TryGetProperty("refresh_token", out var newRt))
            settings["gsc_refresh_token"] = newRt.GetString();

        project.Settings = settings.ToJsonString();
        project.UpdatedAt = DateTime.UtcNow;
        await _projectService.UpdateAsync(project);

        _logger.LogInformation("GSC token refreshed for project {ProjectId}", project.Id);
        return newAccessToken;
    }

    private static string ExtractSlugFromUrl(string url, string fqdn)
    {
        try
        {
            var uri = new Uri(url);
            if (uri.Host.Equals(fqdn, StringComparison.OrdinalIgnoreCase))
                return uri.AbsolutePath.TrimEnd('/');
        }
        catch
        {
            // URL may not contain the expected host — try extracting path portion
            if (url.Contains(fqdn, StringComparison.OrdinalIgnoreCase))
            {
                var idx = url.IndexOf(fqdn, StringComparison.OrdinalIgnoreCase) + fqdn.Length;
                return idx < url.Length ? url[idx..].TrimEnd('/') : "";
            }
        }

        return "";
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
