using System.Text.RegularExpressions;
using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Services;

namespace Contento.Tests.Services;

/// <summary>
/// Tests for <see cref="OEmbedService"/>. The service depends on
/// <see cref="IHttpClientFactory"/> for making oEmbed API requests.
/// These tests focus on:
///   - Constructor guard clauses
///   - Null/empty URL handling
///   - Provider regex pattern matching
///   - ProcessContent behavior for non-embeddable content
/// </summary>
[TestFixture]
public class OEmbedServiceTests
{
    private Mock<IHttpClientFactory> _mockHttpClientFactory = null!;
    private OEmbedService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        // Return a real HttpClient backed by a handler that returns 404 by default.
        // This prevents actual network calls while allowing the service to function.
        _mockHttpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new FakeHttpMessageHandler()));

        _service = new OEmbedService(
            Mock.Of<ILogger<OEmbedService>>(),
            _mockHttpClientFactory.Object);
    }

    // ---------------------------------------------------------------
    // Constructor validation
    // ---------------------------------------------------------------

    [Test]
    public void Constructor_NullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new OEmbedService(null!, _mockHttpClientFactory.Object));
    }

    [Test]
    public void Constructor_NullHttpClientFactory_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new OEmbedService(Mock.Of<ILogger<OEmbedService>>(), null!));
    }

    // ---------------------------------------------------------------
    // ResolveAsync — null/empty/unknown handling
    // ---------------------------------------------------------------

    [Test]
    public async Task ResolveAsync_NullUrl_ReturnsNull()
    {
        var result = await _service.ResolveAsync(null!);
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ResolveAsync_EmptyUrl_ReturnsNull()
    {
        var result = await _service.ResolveAsync(string.Empty);
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ResolveAsync_UnknownUrl_ReturnsNull()
    {
        var result = await _service.ResolveAsync("https://example.com/some-random-page");
        Assert.That(result, Is.Null);
    }

    // ---------------------------------------------------------------
    // ProcessContent — basic behavior
    // ---------------------------------------------------------------

    [Test]
    public void ProcessContent_NullInput_ReturnsEmpty()
    {
        var result = _service.ProcessContent(null!);
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ProcessContent_NoUrls_ReturnsUnchanged()
    {
        var html = "<p>Hello world</p><p>No links here.</p>";
        var result = _service.ProcessContent(html);
        Assert.That(result, Is.EqualTo(html));
    }

    [Test]
    public void ProcessContent_NonEmbeddableUrl_ReturnsUnchanged()
    {
        var html = "<p>https://example.com/just-a-page</p>";
        var result = _service.ProcessContent(html);
        Assert.That(result, Is.EqualTo(html));
    }

    // ---------------------------------------------------------------
    // Provider regex pattern matching — YouTube
    // ---------------------------------------------------------------

    [Test]
    [TestCase("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [TestCase("https://youtube.com/watch?v=dQw4w9WgXcQ")]
    [TestCase("http://www.youtube.com/watch?v=abc123")]
    public void YouTubePattern_MatchesExpectedUrls(string url)
    {
        var matched = OEmbedService.Providers.Any(p => p.Pattern.IsMatch(url));
        Assert.That(matched, Is.True, $"Expected YouTube URL to match: {url}");
    }

    [Test]
    [TestCase("https://youtu.be/dQw4w9WgXcQ")]
    [TestCase("http://youtu.be/abc-123_XYZ")]
    public void YouTubeShortPattern_MatchesExpectedUrls(string url)
    {
        var matched = OEmbedService.Providers.Any(p => p.Pattern.IsMatch(url));
        Assert.That(matched, Is.True, $"Expected youtu.be URL to match: {url}");
    }

    // ---------------------------------------------------------------
    // Provider regex pattern matching — Vimeo
    // ---------------------------------------------------------------

    [Test]
    [TestCase("https://vimeo.com/123456789")]
    [TestCase("https://www.vimeo.com/99999")]
    [TestCase("http://vimeo.com/1")]
    public void VimeoPattern_MatchesExpectedUrls(string url)
    {
        var matched = OEmbedService.Providers.Any(p => p.Pattern.IsMatch(url));
        Assert.That(matched, Is.True, $"Expected Vimeo URL to match: {url}");
    }

    // ---------------------------------------------------------------
    // Provider regex pattern matching — Twitter / X
    // ---------------------------------------------------------------

    [Test]
    [TestCase("https://twitter.com/user/status/1234567890")]
    [TestCase("https://www.twitter.com/elonmusk/status/999")]
    [TestCase("https://x.com/user/status/1234567890")]
    [TestCase("https://www.x.com/user/status/1234567890")]
    public void TwitterPattern_MatchesExpectedUrls(string url)
    {
        var matched = OEmbedService.Providers.Any(p => p.Pattern.IsMatch(url));
        Assert.That(matched, Is.True, $"Expected Twitter/X URL to match: {url}");
    }

    // ---------------------------------------------------------------
    // OEmbedResult default values
    // ---------------------------------------------------------------

    [Test]
    public void OEmbedResult_DefaultValues_AreCorrect()
    {
        var result = new OEmbedResult();

        Assert.That(result.Type, Is.EqualTo(string.Empty));
        Assert.That(result.Html, Is.EqualTo(string.Empty));
        Assert.That(result.Title, Is.Null);
        Assert.That(result.ThumbnailUrl, Is.Null);
        Assert.That(result.ProviderName, Is.EqualTo(string.Empty));
        Assert.That(result.Width, Is.Null);
        Assert.That(result.Height, Is.Null);
    }

    // ---------------------------------------------------------------
    // Service implements IOEmbedService
    // ---------------------------------------------------------------

    [Test]
    public void Service_ImplementsIOEmbedService()
    {
        Assert.That(_service, Is.InstanceOf<IOEmbedService>());
    }

    // ---------------------------------------------------------------
    // Helper: Fake HTTP handler that returns 404 for all requests
    // ---------------------------------------------------------------

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        }
    }
}
