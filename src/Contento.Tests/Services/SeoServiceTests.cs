using System.Data;
using NUnit.Framework;
using Moq;
using Bogus;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Services;

namespace Contento.Tests.Services;

/// <summary>
/// Tests for <see cref="SeoService"/>. The service depends on <see cref="IDbConnection"/>,
/// <see cref="IPostService"/>, and <see cref="ILogger{SeoService}"/>. Since Tuxedo extension
/// methods (QueryAsync, InsertAsync, GetAsync, etc.) on IDbConnection cannot be easily mocked,
/// these tests focus on:
///   - Constructor guard clauses
///   - Method-level argument validation (Guard.Against.*)
///   - GenerateSitemapIndexAsync (synchronous pure logic — fully testable)
///   - Model default values
///   - Verifying the service can be instantiated with valid dependencies
///
/// Methods that exercise the full database path would require an integration test setup.
/// </summary>
[TestFixture]
public class SeoServiceTests
{
    private Mock<IDbConnection> _mockDb = null!;
    private Mock<IPostService> _mockPostService = null!;
    private SeoService _service = null!;
    private Faker _faker = null!;

    [SetUp]
    public void SetUp()
    {
        _mockDb = new Mock<IDbConnection>();
        _mockPostService = new Mock<IPostService>();
        _service = new SeoService(_mockDb.Object, _mockPostService.Object, Mock.Of<ILogger<SeoService>>());
        _faker = new Faker();
    }

    // ---------------------------------------------------------------
    // Constructor validation
    // ---------------------------------------------------------------

    [Test]
    public void Constructor_NullDbConnection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new SeoService(null!, _mockPostService.Object, Mock.Of<ILogger<SeoService>>()));
    }

    [Test]
    public void Constructor_NullPostService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new SeoService(_mockDb.Object, null!, Mock.Of<ILogger<SeoService>>()));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new SeoService(_mockDb.Object, _mockPostService.Object, null!));
    }

    [Test]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        Assert.DoesNotThrow(
            () => new SeoService(_mockDb.Object, _mockPostService.Object, Mock.Of<ILogger<SeoService>>()));
    }

    // ---------------------------------------------------------------
    // AnalyzePostAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void AnalyzePostAsync_DefaultGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.AnalyzePostAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // GetAnalysisAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetAnalysisAsync_DefaultGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetAnalysisAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // GenerateSitemapIndexAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GenerateSitemapIndexAsync_DefaultSiteId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GenerateSitemapIndexAsync(Guid.Empty, "https://example.com"));
    }

    [Test]
    public void GenerateSitemapIndexAsync_NullBaseUrl_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GenerateSitemapIndexAsync(Guid.NewGuid(), null!));
    }

    [Test]
    public void GenerateSitemapIndexAsync_EmptyBaseUrl_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GenerateSitemapIndexAsync(Guid.NewGuid(), ""));
    }

    [Test]
    public void GenerateSitemapIndexAsync_WhitespaceBaseUrl_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GenerateSitemapIndexAsync(Guid.NewGuid(), "   "));
    }

    // ---------------------------------------------------------------
    // GenerateSitemapIndexAsync — output validation (pure/synchronous)
    // ---------------------------------------------------------------

    [Test]
    public async Task GenerateSitemapIndexAsync_ReturnsValidXml_WithSitemapEntries()
    {
        var siteId = Guid.NewGuid();
        var baseUrl = "https://example.com";

        var result = await _service.GenerateSitemapIndexAsync(siteId, baseUrl);

        Assert.That(result, Does.Contain("<?xml version=\"1.0\" encoding=\"UTF-8\"?>"));
        Assert.That(result, Does.Contain("<sitemapindex xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">"));
        Assert.That(result, Does.Contain("</sitemapindex>"));
        Assert.That(result, Does.Contain("<sitemap><loc>https://example.com/sitemap-posts-1.xml</loc></sitemap>"));
        Assert.That(result, Does.Contain("<sitemap><loc>https://example.com/sitemap-categories.xml</loc></sitemap>"));
        Assert.That(result, Does.Contain("<sitemap><loc>https://example.com/sitemap-tags.xml</loc></sitemap>"));
        Assert.That(result, Does.Contain("<sitemap><loc>https://example.com/sitemap-pages.xml</loc></sitemap>"));
    }

    [Test]
    public async Task GenerateSitemapIndexAsync_EscapesSpecialCharacters_InBaseUrl()
    {
        var siteId = Guid.NewGuid();
        var baseUrl = "https://example.com/site&name";

        var result = await _service.GenerateSitemapIndexAsync(siteId, baseUrl);

        // SecurityElement.Escape converts & to &amp;
        Assert.That(result, Does.Contain("https://example.com/site&amp;name/sitemap-posts-1.xml"));
        Assert.That(result, Does.Not.Contain("site&name/sitemap"));
    }

    [Test]
    public async Task GenerateSitemapIndexAsync_TrimsTrailingSlash()
    {
        var siteId = Guid.NewGuid();
        var baseUrl = "https://example.com/";

        var result = await _service.GenerateSitemapIndexAsync(siteId, baseUrl);

        // The trailing slash should be trimmed, so we should NOT see double slashes
        Assert.That(result, Does.Contain("https://example.com/sitemap-posts-1.xml"));
        Assert.That(result, Does.Not.Contain("https://example.com//sitemap"));
    }

    [Test]
    public async Task GenerateSitemapIndexAsync_MultipleTrailingSlashes_TrimsCorrectly()
    {
        var siteId = Guid.NewGuid();
        // TrimEnd('/') trims all trailing slashes
        var baseUrl = "https://example.com///";

        var result = await _service.GenerateSitemapIndexAsync(siteId, baseUrl);

        Assert.That(result, Does.Contain("https://example.com/sitemap-posts-1.xml"));
    }

    // ---------------------------------------------------------------
    // Service implements ISeoService
    // ---------------------------------------------------------------

    [Test]
    public void Service_ImplementsISeoService()
    {
        Assert.That(_service, Is.InstanceOf<ISeoService>());
    }

    // ---------------------------------------------------------------
    // SeoAnalysis default values
    // ---------------------------------------------------------------

    [Test]
    public void SeoAnalysis_DefaultValues_AreCorrect()
    {
        var analysis = new SeoAnalysis();

        Assert.That(analysis.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(analysis.OverallScore, Is.EqualTo(0));
        Assert.That(analysis.Issues, Is.EqualTo("[]"));
        Assert.That(analysis.FocusKeyword, Is.Null);
        Assert.That(analysis.KeywordDensity, Is.Null);
        Assert.That(analysis.ReadabilityScore, Is.Null);
    }

    // ---------------------------------------------------------------
    // GenerateCategorySitemapAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GenerateCategorySitemapAsync_DefaultSiteId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GenerateCategorySitemapAsync(Guid.Empty, "https://example.com"));
    }

    [Test]
    public void GenerateCategorySitemapAsync_NullBaseUrl_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GenerateCategorySitemapAsync(Guid.NewGuid(), null!));
    }

    // ---------------------------------------------------------------
    // GenerateTagSitemapAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GenerateTagSitemapAsync_DefaultSiteId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GenerateTagSitemapAsync(Guid.Empty, "https://example.com"));
    }

    [Test]
    public void GenerateTagSitemapAsync_NullBaseUrl_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GenerateTagSitemapAsync(Guid.NewGuid(), null!));
    }

    // ---------------------------------------------------------------
    // GeneratePageSitemapAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GeneratePageSitemapAsync_DefaultSiteId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GeneratePageSitemapAsync(Guid.Empty, "https://example.com"));
    }

    [Test]
    public void GeneratePageSitemapAsync_NullBaseUrl_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GeneratePageSitemapAsync(Guid.NewGuid(), null!));
    }

    // ---------------------------------------------------------------
    // GeneratePostSitemapAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GeneratePostSitemapAsync_DefaultSiteId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GeneratePostSitemapAsync(Guid.Empty, "https://example.com"));
    }

    [Test]
    public void GeneratePostSitemapAsync_NullBaseUrl_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GeneratePostSitemapAsync(Guid.NewGuid(), null!));
    }
}
