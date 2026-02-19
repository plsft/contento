using System.Data;
using System.Security.Cryptography;
using System.Text;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for privacy-respecting traffic analytics. Records page views with
/// hashed IP addresses and provides aggregated daily statistics and post metrics.
/// </summary>
public class TrafficService : ITrafficService
{
    private readonly IDbConnection _db;
    private readonly ILogger<TrafficService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="TrafficService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="logger">The logger.</param>
    public TrafficService(IDbConnection db, ILogger<TrafficService> logger)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task RecordPageViewAsync(PageView pageView)
    {
        Guard.Against.Null(pageView);

        pageView.Id = Guid.NewGuid();
        pageView.CreatedAt = DateTime.UtcNow;

        // Hash the IP address for privacy -- never store raw IPs
        if (!string.IsNullOrWhiteSpace(pageView.IpHash))
            pageView.IpHash = HashIpAddress(pageView.IpHash);

        await _db.InsertAsync(pageView);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TrafficDaily>> GetDailyStatsAsync(Guid postId, DateTime from, DateTime to)
    {
        Guard.Against.Default(postId);

        return await _db.QueryAsync<TrafficDaily>(
            @"SELECT * FROM traffic_daily
              WHERE post_id = @PostId AND date >= @From AND date <= @To
              ORDER BY date",
            new { PostId = postId, From = from.Date, To = to.Date });
    }

    /// <inheritdoc />
    public async Task<(long TotalViews, long UniqueVisitors, int? AvgTimeOnPageSeconds, decimal? BounceRate)>
        GetPostMetricsAsync(Guid postId, DateTime from, DateTime to)
    {
        Guard.Against.Default(postId);

        var results = await _db.QueryAsync<dynamic>(
            @"SELECT
                COALESCE(SUM(views), 0) AS total_views,
                COALESCE(SUM(unique_visitors), 0) AS unique_visitors,
                AVG(avg_time_on_page_seconds) AS avg_time,
                AVG(bounce_rate) AS avg_bounce
              FROM traffic_daily
              WHERE post_id = @PostId AND date >= @From AND date <= @To",
            new { PostId = postId, From = from.Date, To = to.Date });

        var row = results.FirstOrDefault();
        if (row == null)
            return (0, 0, null, null);

        return (
            (long)(row.total_views ?? 0),
            (long)(row.unique_visitors ?? 0),
            row.avg_time != null ? (int?)Convert.ToInt32(row.avg_time) : null,
            row.avg_bounce != null ? (decimal?)Convert.ToDecimal(row.avg_bounce) : null
        );
    }

    /// <inheritdoc />
    public async Task<(long TotalViews, long UniqueVisitors, IEnumerable<(Guid PostId, long Views)> TopPosts)>
        GetSiteMetricsAsync(Guid siteId, DateTime from, DateTime to)
    {
        Guard.Against.Default(siteId);

        // Get totals
        var totals = await _db.QueryAsync<dynamic>(
            @"SELECT
                COALESCE(SUM(td.views), 0) AS total_views,
                COALESCE(SUM(td.unique_visitors), 0) AS unique_visitors
              FROM traffic_daily td
              INNER JOIN posts p ON p.id = td.post_id
              WHERE p.site_id = @SiteId AND td.date >= @From AND td.date <= @To",
            new { SiteId = siteId, From = from.Date, To = to.Date });

        var totalRow = totals.FirstOrDefault();
        long totalViews = (long)(totalRow?.total_views ?? 0);
        long uniqueVisitors = (long)(totalRow?.unique_visitors ?? 0);

        // Get top posts
        var topPostRows = await _db.QueryAsync<dynamic>(
            @"SELECT td.post_id, SUM(td.views) AS views
              FROM traffic_daily td
              INNER JOIN posts p ON p.id = td.post_id
              WHERE p.site_id = @SiteId AND td.date >= @From AND td.date <= @To
              GROUP BY td.post_id
              ORDER BY views DESC
              LIMIT 10",
            new { SiteId = siteId, From = from.Date, To = to.Date });

        var topPosts = topPostRows.Select(r => ((Guid)r.post_id, (long)r.views)).ToList();

        return (totalViews, uniqueVisitors, topPosts);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<(string Referrer, long Count)>> GetTopReferrersAsync(
        Guid postId, DateTime from, DateTime to, int limit = 10)
    {
        Guard.Against.Default(postId);

        var rows = await _db.QueryAsync<dynamic>(
            @"SELECT referrer, COUNT(*) AS cnt
              FROM page_views
              WHERE post_id = @PostId AND created_at >= @From AND created_at <= @To
                AND referrer IS NOT NULL AND referrer != ''
              GROUP BY referrer
              ORDER BY cnt DESC
              LIMIT @Limit",
            new { PostId = postId, From = from, To = to, Limit = limit });

        return rows.Select(r => ((string)r.referrer, (long)r.cnt));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<(string DeviceType, long Count)>> GetDeviceBreakdownAsync(
        Guid postId, DateTime from, DateTime to)
    {
        Guard.Against.Default(postId);

        var rows = await _db.QueryAsync<dynamic>(
            @"SELECT COALESCE(device_type, 'unknown') AS device_type, COUNT(*) AS cnt
              FROM page_views
              WHERE post_id = @PostId AND created_at >= @From AND created_at <= @To
              GROUP BY device_type
              ORDER BY cnt DESC",
            new { PostId = postId, From = from, To = to });

        return rows.Select(r => ((string)r.device_type, (long)r.cnt));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<(string CountryCode, long Count)>> GetCountryBreakdownAsync(
        Guid postId, DateTime from, DateTime to, int limit = 10)
    {
        Guard.Against.Default(postId);

        var rows = await _db.QueryAsync<dynamic>(
            @"SELECT COALESCE(country_code, '??') AS country_code, COUNT(*) AS cnt
              FROM page_views
              WHERE post_id = @PostId AND created_at >= @From AND created_at <= @To
              GROUP BY country_code
              ORDER BY cnt DESC
              LIMIT @Limit",
            new { PostId = postId, From = from, To = to, Limit = limit });

        return rows.Select(r => ((string)r.country_code, (long)r.cnt));
    }

    /// <summary>
    /// Hashes an IP address using SHA-256 for privacy-preserving unique visitor counting.
    /// The hash is one-way and cannot be reversed to the original IP.
    /// </summary>
    private static string HashIpAddress(string ipAddress)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(ipAddress));
        return Convert.ToHexStringLower(bytes);
    }
}
