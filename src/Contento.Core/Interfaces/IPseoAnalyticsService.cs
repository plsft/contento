using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for Google Search Console analytics integration — OAuth, sync, querying, and export.
/// </summary>
public interface IPseoAnalyticsService
{
    // GSC OAuth
    Task<string> GetGscAuthUrlAsync(Guid projectId, string redirectUri);
    Task ExchangeGscCodeAsync(Guid projectId, string code, string redirectUri);

    // Sync
    Task SyncGscDataAsync(Guid projectId, CancellationToken ct = default);

    // Query
    Task<PseoAnalyticsSummary> GetProjectSummaryAsync(Guid projectId, int days = 30);
    Task<List<PseoPageAnalytics>> GetPageAnalyticsAsync(Guid projectId, int days = 30, int page = 1, int pageSize = 50);
    Task<List<PseoPageAnalytics>> GetTopPagesAsync(Guid projectId, int days = 30, int limit = 10);
    Task<List<NichePerformance>> GetNichePerformanceAsync(Guid projectId, int days = 30);
    Task<List<PseoPage>> GetZeroTrafficPagesAsync(Guid projectId, int daysSincePublish = 60);

    // Export
    Task<byte[]> ExportCsvAsync(Guid projectId, int days = 30);
}

// DTOs

/// <summary>
/// Aggregated analytics summary for a pSEO project.
/// </summary>
public class PseoAnalyticsSummary
{
    public int TotalPages { get; set; }
    public int PublishedPages { get; set; }
    public int IndexedPages { get; set; }
    public int TotalClicks { get; set; }
    public int TotalImpressions { get; set; }
    public decimal AvgCtr { get; set; }
    public decimal AvgPosition { get; set; }
    public List<DailyTraffic> DailyTraffic { get; set; } = [];
}

/// <summary>
/// Clicks and impressions for a single day.
/// </summary>
public class DailyTraffic
{
    public DateTime Date { get; set; }
    public int Clicks { get; set; }
    public int Impressions { get; set; }
}

/// <summary>
/// Analytics data for a single pSEO page.
/// </summary>
public class PseoPageAnalytics
{
    public Guid PageId { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public int Clicks { get; set; }
    public int Impressions { get; set; }
    public decimal Ctr { get; set; }
    public decimal Position { get; set; }
    public bool IsIndexed { get; set; }
}

/// <summary>
/// Aggregated search performance for a niche within a project.
/// </summary>
public class NichePerformance
{
    public string NicheSlug { get; set; } = "";
    public int PageCount { get; set; }
    public int TotalClicks { get; set; }
    public int TotalImpressions { get; set; }
    public decimal AvgPosition { get; set; }
}
