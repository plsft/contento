using System.Data;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Moq;
using Bogus;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Services;

namespace Contento.Tests.Services;

/// <summary>
/// Tests for <see cref="SpamService"/>. The service depends on <see cref="IDbConnection"/>
/// and <see cref="ILogger{SpamService}"/>. Since Tuxedo extension methods (QueryAsync,
/// InsertAsync, ExecuteScalarAsync, etc.) on IDbConnection cannot be easily mocked, these
/// tests focus on:
///   - Constructor guard clauses
///   - Method-level argument validation (Guard.Against.*)
///   - Model default values
///   - Spam pattern regex coverage
///   - Verifying the service can be instantiated with valid dependencies
///
/// Methods that exercise the full database path would require an integration test setup.
/// </summary>
[TestFixture]
public class SpamServiceTests
{
    private Mock<IDbConnection> _mockDb = null!;
    private SpamService _service = null!;
    private Faker _faker = null!;

    [SetUp]
    public void SetUp()
    {
        _mockDb = new Mock<IDbConnection>();
        _service = new SpamService(_mockDb.Object, Mock.Of<ILogger<SpamService>>());
        _faker = new Faker();
    }

    // ---------------------------------------------------------------
    // Constructor validation
    // ---------------------------------------------------------------

    [Test]
    public void Constructor_NullDbConnection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new SpamService(null!, Mock.Of<ILogger<SpamService>>()));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new SpamService(_mockDb.Object, null!));
    }

    [Test]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        Assert.DoesNotThrow(
            () => new SpamService(_mockDb.Object, Mock.Of<ILogger<SpamService>>()));
    }

    // ---------------------------------------------------------------
    // CheckCommentAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void CheckCommentAsync_NullComment_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.CheckCommentAsync(null!));
    }

    // ---------------------------------------------------------------
    // TrainHamAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void TrainHamAsync_DefaultGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.TrainHamAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // TrainSpamAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void TrainSpamAsync_DefaultGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.TrainSpamAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // GetStatsAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetStatsAsync_DefaultGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetStatsAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // Service implements ISpamService
    // ---------------------------------------------------------------

    [Test]
    public void Service_ImplementsISpamService()
    {
        Assert.That(_service, Is.InstanceOf<ISpamService>());
    }

    // ---------------------------------------------------------------
    // SpamCheckResult default values
    // ---------------------------------------------------------------

    [Test]
    public void SpamCheckResult_DefaultValues_AreCorrect()
    {
        var result = new SpamCheckResult();

        Assert.That(result.IsSpam, Is.False);
        Assert.That(result.Score, Is.EqualTo(0m));
        Assert.That(result.Reasons, Is.Not.Null);
        Assert.That(result.Reasons, Is.Empty);
    }

    // ---------------------------------------------------------------
    // SpamStats default values
    // ---------------------------------------------------------------

    [Test]
    public void SpamStats_DefaultValues_AreCorrect()
    {
        var stats = new SpamStats();

        Assert.That(stats.TotalChecked, Is.EqualTo(0));
        Assert.That(stats.TotalBlocked, Is.EqualTo(0));
        Assert.That(stats.TotalApproved, Is.EqualTo(0));
    }

    // ---------------------------------------------------------------
    // Spam pattern regex — known spam words should match
    // ---------------------------------------------------------------

    [Test]
    [TestCase("Buy cheap viagra online today")]
    [TestCase("Visit our casino for great deals")]
    [TestCase("cialis pills at lowest price")]
    [TestCase("crypto invest opportunity now")]
    [TestCase("Click here now to claim your prize")]
    [TestCase("Act now before it is too late")]
    [TestCase("Limited time offer just for you")]
    [TestCase("Make money fast with this trick")]
    public void SpamPatternRegex_KnownSpamPhrases_ShouldMatch(string input)
    {
        // The SpamService uses this compiled regex internally. We replicate it here
        // to verify the patterns that the service's scoring engine relies on.
        var spamPatternRegex = new Regex(
            @"casino|viagra|cialis|crypto.*invest|buy.*cheap|click here now|act now|limited time|make money fast|work from home.*earn|SEO.*rank|backlink.*service",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        Assert.That(spamPatternRegex.IsMatch(input), Is.True,
            $"Expected spam pattern to match: '{input}'");
    }

    [Test]
    [TestCase("This is a great blog post about gardening")]
    [TestCase("I really enjoyed reading this article")]
    [TestCase("Thanks for sharing your thoughts on the topic")]
    public void SpamPatternRegex_LegitimateComments_ShouldNotMatch(string input)
    {
        var spamPatternRegex = new Regex(
            @"casino|viagra|cialis|crypto.*invest|buy.*cheap|click here now|act now|limited time|make money fast|work from home.*earn|SEO.*rank|backlink.*service",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        Assert.That(spamPatternRegex.IsMatch(input), Is.False,
            $"Expected spam pattern NOT to match: '{input}'");
    }

    // ---------------------------------------------------------------
    // Multiple instances can be created independently
    // ---------------------------------------------------------------

    [Test]
    public void MultipleInstances_CanBeCreatedIndependently()
    {
        var db1 = new Mock<IDbConnection>();
        var db2 = new Mock<IDbConnection>();

        var service1 = new SpamService(db1.Object, Mock.Of<ILogger<SpamService>>());
        var service2 = new SpamService(db2.Object, Mock.Of<ILogger<SpamService>>());

        Assert.That(service1, Is.Not.SameAs(service2));
        Assert.That(service1, Is.InstanceOf<ISpamService>());
        Assert.That(service2, Is.InstanceOf<ISpamService>());
    }

    // ---------------------------------------------------------------
    // SpamScore model defaults (model used by the service)
    // ---------------------------------------------------------------

    [Test]
    public void SpamScore_DefaultValues_AreCorrect()
    {
        var spamScore = new SpamScore();

        Assert.That(spamScore.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(spamScore.Score, Is.EqualTo(0m));
        Assert.That(spamScore.Reasons, Is.EqualTo("[]"));
        Assert.That(spamScore.IsSpam, Is.False);
    }

    // ---------------------------------------------------------------
    // Comment model used by CheckCommentAsync
    // ---------------------------------------------------------------

    [Test]
    public void Comment_DefaultValues_AreCorrect()
    {
        var comment = new Comment();

        Assert.That(comment.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(comment.AuthorName, Is.EqualTo(string.Empty));
        Assert.That(comment.BodyMarkdown, Is.EqualTo(string.Empty));
        Assert.That(comment.Status, Is.EqualTo("pending"));
        Assert.That(comment.AuthorUrl, Is.Null);
        Assert.That(comment.AuthorEmail, Is.Null);
        Assert.That(comment.IpAddress, Is.Null);
    }
}
