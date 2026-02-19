using NUnit.Framework;
using Bogus;
using Microsoft.AspNetCore.Http;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Tests.Middleware;

/// <summary>
/// Tests for <see cref="HttpContextExtensions"/>. These static extension methods provide
/// typed access to the current <see cref="Site"/> stored in <see cref="HttpContext.Items"/>
/// by the SiteResolutionMiddleware. Tests use <see cref="DefaultHttpContext"/> to simulate
/// the HttpContext without requiring a full middleware pipeline.
/// </summary>
[TestFixture]
public class SiteResolutionTests
{
    private Faker _faker = null!;

    [SetUp]
    public void SetUp()
    {
        _faker = new Faker();
    }

    // ---------------------------------------------------------------
    // GetCurrentSite
    // ---------------------------------------------------------------

    [Test]
    public void GetCurrentSite_WithSiteInItems_ReturnsSite()
    {
        var ctx = new DefaultHttpContext();
        var site = new Site { Id = Guid.NewGuid(), Name = _faker.Company.CompanyName(), Slug = _faker.Lorem.Slug() };
        ctx.Items["CurrentSite"] = site;

        var result = ctx.GetCurrentSite();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(site.Id));
        Assert.That(result.Name, Is.EqualTo(site.Name));
    }

    [Test]
    public void GetCurrentSite_WithoutSiteInItems_ThrowsInvalidOperationException()
    {
        var ctx = new DefaultHttpContext();

        Assert.Throws<InvalidOperationException>(() => ctx.GetCurrentSite());
    }

    [Test]
    public void GetCurrentSite_WithWrongTypeInItems_ThrowsInvalidOperationException()
    {
        var ctx = new DefaultHttpContext();
        ctx.Items["CurrentSite"] = "not-a-site";

        Assert.Throws<InvalidOperationException>(() => ctx.GetCurrentSite());
    }

    [Test]
    public void GetCurrentSite_ReturnsSameSiteInstance()
    {
        var ctx = new DefaultHttpContext();
        var site = new Site { Id = Guid.NewGuid(), Name = _faker.Company.CompanyName(), Slug = _faker.Lorem.Slug() };
        ctx.Items["CurrentSite"] = site;

        var result1 = ctx.GetCurrentSite();
        var result2 = ctx.GetCurrentSite();

        Assert.That(result1, Is.SameAs(result2));
        Assert.That(result1, Is.SameAs(site));
    }

    // ---------------------------------------------------------------
    // GetCurrentSiteId
    // ---------------------------------------------------------------

    [Test]
    public void GetCurrentSiteId_WithSiteInItems_ReturnsSiteId()
    {
        var ctx = new DefaultHttpContext();
        var expectedId = Guid.NewGuid();
        var site = new Site { Id = expectedId, Name = _faker.Company.CompanyName(), Slug = _faker.Lorem.Slug() };
        ctx.Items["CurrentSite"] = site;

        var result = ctx.GetCurrentSiteId();

        Assert.That(result, Is.EqualTo(expectedId));
    }

    [Test]
    public void GetCurrentSiteId_WithoutSiteInItems_ThrowsInvalidOperationException()
    {
        var ctx = new DefaultHttpContext();

        Assert.Throws<InvalidOperationException>(() => ctx.GetCurrentSiteId());
    }

    [Test]
    public void GetCurrentSiteId_ReturnsCorrectGuid()
    {
        var ctx = new DefaultHttpContext();
        var knownId = Guid.NewGuid();
        var site = new Site { Id = knownId, Name = "Test Site", Slug = "test-site" };
        ctx.Items["CurrentSite"] = site;

        var result = ctx.GetCurrentSiteId();

        Assert.That(result, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result, Is.EqualTo(knownId));
    }

    // ---------------------------------------------------------------
    // TryGetCurrentSite
    // ---------------------------------------------------------------

    [Test]
    public void TryGetCurrentSite_WithSiteInItems_ReturnsSite()
    {
        var ctx = new DefaultHttpContext();
        var site = new Site { Id = Guid.NewGuid(), Name = _faker.Company.CompanyName(), Slug = _faker.Lorem.Slug() };
        ctx.Items["CurrentSite"] = site;

        var result = ctx.TryGetCurrentSite();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(site.Id));
    }

    [Test]
    public void TryGetCurrentSite_WithoutSiteInItems_ReturnsNull()
    {
        var ctx = new DefaultHttpContext();

        var result = ctx.TryGetCurrentSite();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void TryGetCurrentSite_WithWrongTypeInItems_ReturnsNull()
    {
        var ctx = new DefaultHttpContext();
        ctx.Items["CurrentSite"] = "not-a-site";

        var result = ctx.TryGetCurrentSite();

        Assert.That(result, Is.Null);
    }

    // ---------------------------------------------------------------
    // Consistency / multiple calls
    // ---------------------------------------------------------------

    [Test]
    public void MultipleCallsReturnSameResult()
    {
        var ctx = new DefaultHttpContext();
        var site = new Site { Id = Guid.NewGuid(), Name = _faker.Company.CompanyName(), Slug = _faker.Lorem.Slug() };
        ctx.Items["CurrentSite"] = site;

        var getSite1 = ctx.GetCurrentSite();
        var getSite2 = ctx.GetCurrentSite();
        var getId1 = ctx.GetCurrentSiteId();
        var getId2 = ctx.GetCurrentSiteId();
        var trySite1 = ctx.TryGetCurrentSite();
        var trySite2 = ctx.TryGetCurrentSite();

        Assert.That(getSite1, Is.SameAs(getSite2));
        Assert.That(getId1, Is.EqualTo(getId2));
        Assert.That(trySite1, Is.SameAs(trySite2));
        Assert.That(getSite1, Is.SameAs(trySite1));
    }
}
