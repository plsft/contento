using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for privacy-respecting traffic analytics.
/// Records page views, provides daily aggregated statistics, and post/site-level metrics.
/// </summary>
public interface ITrafficService
{
    /// <summary>
    /// Records a page view event. IP addresses are hashed for unique visitor counting;
    /// raw IPs are never stored.
    /// </summary>
    /// <param name="pageView">The page view record to persist.</param>
    Task RecordPageViewAsync(PageView pageView);

    /// <summary>
    /// Retrieves aggregated daily traffic statistics for a specific post within a date range.
    /// </summary>
    /// <param name="postId">The post identifier.</param>
    /// <param name="from">The start date (inclusive).</param>
    /// <param name="to">The end date (inclusive).</param>
    /// <returns>A collection of daily traffic records for the post.</returns>
    Task<IEnumerable<TrafficDaily>> GetDailyStatsAsync(Guid postId, DateTime from, DateTime to);

    /// <summary>
    /// Retrieves traffic metrics summary for a specific post:
    /// total views, unique visitors, average time on page, and bounce rate.
    /// </summary>
    /// <param name="postId">The post identifier.</param>
    /// <param name="from">The start date (inclusive).</param>
    /// <param name="to">The end date (inclusive).</param>
    /// <returns>A tuple containing total views, unique visitors, average time on page, and bounce rate.</returns>
    Task<(long TotalViews, long UniqueVisitors, int? AvgTimeOnPageSeconds, decimal? BounceRate)> GetPostMetricsAsync(
        Guid postId, DateTime from, DateTime to);

    /// <summary>
    /// Retrieves site-wide traffic metrics across all posts within a date range.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="from">The start date (inclusive).</param>
    /// <param name="to">The end date (inclusive).</param>
    /// <returns>A tuple containing total views, unique visitors, and the top posts by views.</returns>
    Task<(long TotalViews, long UniqueVisitors, IEnumerable<(Guid PostId, long Views)> TopPosts)> GetSiteMetricsAsync(
        Guid siteId, DateTime from, DateTime to);

    /// <summary>
    /// Retrieves the top referrers for a specific post within a date range.
    /// </summary>
    /// <param name="postId">The post identifier.</param>
    /// <param name="from">The start date (inclusive).</param>
    /// <param name="to">The end date (inclusive).</param>
    /// <param name="limit">Maximum number of referrers to return.</param>
    /// <returns>A collection of referrer URLs with their view counts.</returns>
    Task<IEnumerable<(string Referrer, long Count)>> GetTopReferrersAsync(Guid postId, DateTime from, DateTime to,
        int limit = 10);

    /// <summary>
    /// Retrieves the device type breakdown for a specific post within a date range.
    /// </summary>
    /// <param name="postId">The post identifier.</param>
    /// <param name="from">The start date (inclusive).</param>
    /// <param name="to">The end date (inclusive).</param>
    /// <returns>A collection of device types with their view counts.</returns>
    Task<IEnumerable<(string DeviceType, long Count)>> GetDeviceBreakdownAsync(Guid postId, DateTime from, DateTime to);

    /// <summary>
    /// Retrieves the country breakdown for a specific post within a date range.
    /// </summary>
    /// <param name="postId">The post identifier.</param>
    /// <param name="from">The start date (inclusive).</param>
    /// <param name="to">The end date (inclusive).</param>
    /// <param name="limit">Maximum number of countries to return.</param>
    /// <returns>A collection of country codes with their view counts.</returns>
    Task<IEnumerable<(string CountryCode, long Count)>> GetCountryBreakdownAsync(Guid postId, DateTime from, DateTime to,
        int limit = 10);
}
