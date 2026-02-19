using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// SEO analysis and sitemap generation service.
/// </summary>
public interface ISeoService
{
    Task<SeoAnalysis> AnalyzePostAsync(Guid postId, string? focusKeyword = null);
    Task<SeoAnalysis?> GetAnalysisAsync(Guid postId);
    Task<string> GenerateSitemapIndexAsync(Guid siteId, string baseUrl);
    Task<string> GeneratePostSitemapAsync(Guid siteId, string baseUrl, int page = 1);
    Task<string> GenerateCategorySitemapAsync(Guid siteId, string baseUrl);
    Task<string> GenerateTagSitemapAsync(Guid siteId, string baseUrl);
    Task<string> GeneratePageSitemapAsync(Guid siteId, string baseUrl);
}
