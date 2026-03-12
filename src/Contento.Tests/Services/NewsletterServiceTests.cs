using System.Data;
using NUnit.Framework;
using Moq;
using Bogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Services;

namespace Contento.Tests.Services;

/// <summary>
/// Tests for <see cref="NewsletterService"/>. The service depends on <see cref="IDbConnection"/>,
/// <see cref="IEmailService"/>, <see cref="IPostService"/>, <see cref="IMarkdownService"/>,
/// <see cref="ISiteService"/>, <see cref="IConfiguration"/>, and <see cref="ILogger{T}"/>.
/// Since Tuxedo extension methods (QueryAsync, InsertAsync, etc.) on IDbConnection cannot be
/// easily mocked, these tests focus on:
///   - Constructor guard clauses
///   - Method-level argument validation (Guard.Against.*)
///   - Verifying the service can be instantiated with valid dependencies
///
/// Methods that exercise the full database path would require an integration test setup.
/// </summary>
[TestFixture]
public class NewsletterServiceTests
{
    private Mock<IDbConnection> _mockDb = null!;
    private Mock<IEmailService> _mockEmailService = null!;
    private Mock<IPostService> _mockPostService = null!;
    private Mock<IMarkdownService> _mockMarkdownService = null!;
    private Mock<ISiteService> _mockSiteService = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private NewsletterService _service = null!;
    private Faker _faker = null!;

    [SetUp]
    public void SetUp()
    {
        _mockDb = new Mock<IDbConnection>();
        _mockEmailService = new Mock<IEmailService>();
        _mockPostService = new Mock<IPostService>();
        _mockMarkdownService = new Mock<IMarkdownService>();
        _mockSiteService = new Mock<ISiteService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _service = new NewsletterService(
            _mockDb.Object,
            _mockEmailService.Object,
            _mockPostService.Object,
            _mockMarkdownService.Object,
            _mockSiteService.Object,
            _mockConfiguration.Object,
            Mock.Of<ILogger<NewsletterService>>());
        _faker = new Faker();
    }

    // ---------------------------------------------------------------
    // Constructor validation
    // ---------------------------------------------------------------

    [Test]
    public void Constructor_NullDbConnection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new NewsletterService(
                null!,
                _mockEmailService.Object,
                _mockPostService.Object,
                _mockMarkdownService.Object,
                _mockSiteService.Object,
                _mockConfiguration.Object,
                Mock.Of<ILogger<NewsletterService>>()));
    }

    [Test]
    public void Constructor_NullEmailService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new NewsletterService(
                _mockDb.Object,
                null!,
                _mockPostService.Object,
                _mockMarkdownService.Object,
                _mockSiteService.Object,
                _mockConfiguration.Object,
                Mock.Of<ILogger<NewsletterService>>()));
    }

    [Test]
    public void Constructor_NullPostService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new NewsletterService(
                _mockDb.Object,
                _mockEmailService.Object,
                null!,
                _mockMarkdownService.Object,
                _mockSiteService.Object,
                _mockConfiguration.Object,
                Mock.Of<ILogger<NewsletterService>>()));
    }

    [Test]
    public void Constructor_NullMarkdownService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new NewsletterService(
                _mockDb.Object,
                _mockEmailService.Object,
                _mockPostService.Object,
                null!,
                _mockSiteService.Object,
                _mockConfiguration.Object,
                Mock.Of<ILogger<NewsletterService>>()));
    }

    [Test]
    public void Constructor_NullSiteService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new NewsletterService(
                _mockDb.Object,
                _mockEmailService.Object,
                _mockPostService.Object,
                _mockMarkdownService.Object,
                null!,
                _mockConfiguration.Object,
                Mock.Of<ILogger<NewsletterService>>()));
    }

    [Test]
    public void Constructor_NullConfiguration_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new NewsletterService(
                _mockDb.Object,
                _mockEmailService.Object,
                _mockPostService.Object,
                _mockMarkdownService.Object,
                _mockSiteService.Object,
                null!,
                Mock.Of<ILogger<NewsletterService>>()));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new NewsletterService(
                _mockDb.Object,
                _mockEmailService.Object,
                _mockPostService.Object,
                _mockMarkdownService.Object,
                _mockSiteService.Object,
                _mockConfiguration.Object,
                null!));
    }

    [Test]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        Assert.DoesNotThrow(
            () => new NewsletterService(
                _mockDb.Object,
                _mockEmailService.Object,
                _mockPostService.Object,
                _mockMarkdownService.Object,
                _mockSiteService.Object,
                _mockConfiguration.Object,
                Mock.Of<ILogger<NewsletterService>>()));
    }

    // ---------------------------------------------------------------
    // SubscribeAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void SubscribeAsync_DefaultSiteId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.SubscribeAsync(Guid.Empty, "test@example.com"));
    }

    [Test]
    public void SubscribeAsync_NullEmail_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.SubscribeAsync(Guid.NewGuid(), null!));
    }

    [Test]
    public void SubscribeAsync_EmptyEmail_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.SubscribeAsync(Guid.NewGuid(), ""));
    }

    [Test]
    public void SubscribeAsync_WhitespaceEmail_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.SubscribeAsync(Guid.NewGuid(), "   "));
    }

    // ---------------------------------------------------------------
    // UnsubscribeAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void UnsubscribeAsync_NullToken_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.UnsubscribeAsync(null!));
    }

    [Test]
    public void UnsubscribeAsync_EmptyToken_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.UnsubscribeAsync(""));
    }

    [Test]
    public void UnsubscribeAsync_WhitespaceToken_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.UnsubscribeAsync("   "));
    }

    // ---------------------------------------------------------------
    // GetActiveSubscribersAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetActiveSubscribersAsync_DefaultSiteId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetActiveSubscribersAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // GetSubscriberCountAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetSubscriberCountAsync_DefaultSiteId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetSubscriberCountAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // SendCampaignAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void SendCampaignAsync_DefaultSiteId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.SendCampaignAsync(Guid.Empty, Guid.NewGuid()));
    }

    [Test]
    public void SendCampaignAsync_DefaultPostId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.SendCampaignAsync(Guid.NewGuid(), Guid.Empty));
    }

    // ---------------------------------------------------------------
    // GetCampaignAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetCampaignAsync_DefaultCampaignId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetCampaignAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // GetCampaignsAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetCampaignsAsync_DefaultSiteId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetCampaignsAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // Bogus-generated data for fuzz-style guard validation
    // ---------------------------------------------------------------

    [Test]
    public void SubscribeAsync_BogusData_WithValidFields_PassesGuards()
    {
        var siteId = Guid.NewGuid();
        var email = _faker.Internet.Email();

        // The guard clauses should not throw for valid input. The actual DB call
        // will throw because IDbConnection is a mock without Tuxedo extension wiring,
        // so we expect a different exception type.
        var ex = Assert.CatchAsync(async () => await _service.SubscribeAsync(siteId, email));

        // If an exception occurs, it should NOT be ArgumentNullException or ArgumentException
        // (those would indicate a guard failure)
        if (ex != null)
        {
            Assert.That(ex, Is.Not.TypeOf<ArgumentNullException>());
            Assert.That(ex, Is.Not.TypeOf<ArgumentException>());
        }
    }

    [Test]
    public void UnsubscribeAsync_BogusToken_PassesGuards()
    {
        var token = _faker.Random.AlphaNumeric(64);

        var ex = Assert.CatchAsync(async () => await _service.UnsubscribeAsync(token));

        if (ex != null)
        {
            Assert.That(ex, Is.Not.TypeOf<ArgumentNullException>());
            Assert.That(ex, Is.Not.TypeOf<ArgumentException>());
        }
    }
}
