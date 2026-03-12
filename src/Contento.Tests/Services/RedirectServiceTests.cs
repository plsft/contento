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
/// Tests for <see cref="RedirectService"/>. The service depends on <see cref="IDbConnection"/>
/// and <see cref="ILogger{RedirectService}"/>. Since Tuxedo extension methods (QueryAsync,
/// InsertAsync, etc.) on IDbConnection cannot be easily mocked, these tests focus on:
///   - Constructor guard clauses
///   - Method-level argument validation (Guard.Against.*)
///   - Verifying the service can be instantiated with valid dependencies
///   - Model defaults
/// </summary>
[TestFixture]
public class RedirectServiceTests
{
    private Mock<IDbConnection> _mockDb = null!;
    private RedirectService _service = null!;
    private Faker _faker = null!;

    [SetUp]
    public void SetUp()
    {
        _mockDb = new Mock<IDbConnection>();
        _service = new RedirectService(_mockDb.Object, Mock.Of<ILogger<RedirectService>>());
        _faker = new Faker();
    }

    // ---------------------------------------------------------------
    // Constructor validation
    // ---------------------------------------------------------------

    [Test]
    public void Constructor_NullDb_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new RedirectService(null!, Mock.Of<ILogger<RedirectService>>()));
    }

    [Test]
    public void Constructor_NullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new RedirectService(_mockDb.Object, null!));
    }

    [Test]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        Assert.DoesNotThrow(
            () => new RedirectService(_mockDb.Object, Mock.Of<ILogger<RedirectService>>()));
    }

    // ---------------------------------------------------------------
    // GetByIdAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetByIdAsync_EmptyGuid_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetByIdAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // GetAllAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetAllAsync_EmptyGuid_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetAllAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // GetByFromPathAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetByFromPathAsync_EmptyGuid_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetByFromPathAsync(Guid.Empty, "/some-path"));
    }

    [Test]
    public void GetByFromPathAsync_NullPath_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetByFromPathAsync(Guid.NewGuid(), null!));
    }

    [Test]
    public void GetByFromPathAsync_EmptyPath_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetByFromPathAsync(Guid.NewGuid(), ""));
    }

    [Test]
    public void GetByFromPathAsync_WhitespacePath_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetByFromPathAsync(Guid.NewGuid(), "   "));
    }

    // ---------------------------------------------------------------
    // CreateAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void CreateAsync_NullRedirect_Throws()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.CreateAsync(null!));
    }

    [Test]
    public void CreateAsync_EmptyFromPath_Throws()
    {
        var redirect = CreateValidRedirect();
        redirect.FromPath = "";

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(redirect));
    }

    [Test]
    public void CreateAsync_EmptyToPath_Throws()
    {
        var redirect = CreateValidRedirect();
        redirect.ToPath = "";

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(redirect));
    }

    [Test]
    public void CreateAsync_DefaultSiteId_Throws()
    {
        var redirect = CreateValidRedirect();
        redirect.SiteId = Guid.Empty;

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(redirect));
    }

    // ---------------------------------------------------------------
    // DeleteAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void DeleteAsync_EmptyGuid_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.DeleteAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // IncrementHitCountAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void IncrementHitCountAsync_EmptyGuid_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.IncrementHitCountAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // GetTotalCountAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetTotalCountAsync_EmptyGuid_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetTotalCountAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // CreateSlugChangeRedirectAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void CreateSlugChangeRedirectAsync_EmptyGuid_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateSlugChangeRedirectAsync(Guid.Empty, "old-slug", "new-slug"));
    }

    [Test]
    public void CreateSlugChangeRedirectAsync_NullOldSlug_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateSlugChangeRedirectAsync(Guid.NewGuid(), null!, "new-slug"));
    }

    [Test]
    public void CreateSlugChangeRedirectAsync_EmptyOldSlug_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateSlugChangeRedirectAsync(Guid.NewGuid(), "", "new-slug"));
    }

    [Test]
    public void CreateSlugChangeRedirectAsync_NullNewSlug_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateSlugChangeRedirectAsync(Guid.NewGuid(), "old-slug", null!));
    }

    [Test]
    public void CreateSlugChangeRedirectAsync_EmptyNewSlug_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateSlugChangeRedirectAsync(Guid.NewGuid(), "old-slug", ""));
    }

    // ---------------------------------------------------------------
    // Interface implementation
    // ---------------------------------------------------------------

    [Test]
    public void Service_ImplementsIRedirectService()
    {
        Assert.That(_service, Is.InstanceOf<IRedirectService>());
    }

    // ---------------------------------------------------------------
    // Multiple instances
    // ---------------------------------------------------------------

    [Test]
    public void MultipleInstances_CanBeCreatedIndependently()
    {
        var db1 = new Mock<IDbConnection>();
        var db2 = new Mock<IDbConnection>();

        var service1 = new RedirectService(db1.Object, Mock.Of<ILogger<RedirectService>>());
        var service2 = new RedirectService(db2.Object, Mock.Of<ILogger<RedirectService>>());

        Assert.That(service1, Is.Not.SameAs(service2));
    }

    // ---------------------------------------------------------------
    // Redirect model defaults
    // ---------------------------------------------------------------

    [Test]
    public void Redirect_DefaultValues_AreCorrect()
    {
        var redirect = new Redirect();

        Assert.That(redirect.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(redirect.FromPath, Is.EqualTo(string.Empty));
        Assert.That(redirect.ToPath, Is.EqualTo(string.Empty));
        Assert.That(redirect.StatusCode, Is.EqualTo(301));
        Assert.That(redirect.IsActive, Is.True);
        Assert.That(redirect.HitCount, Is.EqualTo(0));
        Assert.That(redirect.LastHitAt, Is.Null);
        Assert.That(redirect.Notes, Is.Null);
    }

    [Test]
    public void Redirect_FieldsAreSettable()
    {
        var redirect = new Redirect
        {
            Id = Guid.NewGuid(),
            SiteId = Guid.NewGuid(),
            FromPath = "/old-page",
            ToPath = "/new-page",
            StatusCode = 302,
            IsActive = false,
            HitCount = 42,
            LastHitAt = DateTime.UtcNow,
            Notes = _faker.Lorem.Sentence()
        };

        Assert.That(redirect.FromPath, Is.EqualTo("/old-page"));
        Assert.That(redirect.ToPath, Is.EqualTo("/new-page"));
        Assert.That(redirect.StatusCode, Is.EqualTo(302));
        Assert.That(redirect.IsActive, Is.False);
        Assert.That(redirect.HitCount, Is.EqualTo(42));
        Assert.That(redirect.LastHitAt, Is.Not.Null);
        Assert.That(redirect.Notes, Is.Not.Null);
    }

    // ---------------------------------------------------------------
    // Bogus-generated data for fuzz-style guard validation
    // ---------------------------------------------------------------

    [Test]
    public void CreateAsync_BogusRedirect_WithValidFields_PassesGuards()
    {
        var redirect = new Redirect
        {
            SiteId = Guid.NewGuid(),
            FromPath = "/" + _faker.Internet.DomainWord(),
            ToPath = "/" + _faker.Internet.DomainWord()
        };

        var ex = Assert.CatchAsync(async () => await _service.CreateAsync(redirect));

        if (ex != null)
        {
            Assert.That(ex, Is.Not.TypeOf<ArgumentNullException>());
            Assert.That(ex, Is.Not.TypeOf<ArgumentException>());
        }
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private Redirect CreateValidRedirect()
    {
        return new Redirect
        {
            SiteId = Guid.NewGuid(),
            FromPath = "/old-page",
            ToPath = "/new-page",
            StatusCode = 301,
            IsActive = true
        };
    }
}
