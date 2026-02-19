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
/// Tests for <see cref="MembershipPlanService"/>. The service depends on <see cref="IDbConnection"/>
/// and <see cref="ILogger{T}"/>. Since Tuxedo extension methods (GetAsync, QueryAsync, InsertAsync,
/// etc.) on IDbConnection cannot be easily mocked, these tests focus on:
///   - Constructor guard clauses
///   - Method-level argument validation (Guard.Against.*)
///   - Verifying the service can be instantiated with valid dependencies
///
/// Methods that exercise the full database path would require an integration test setup.
/// </summary>
[TestFixture]
public class MembershipPlanServiceTests
{
    private Mock<IDbConnection> _mockDb = null!;
    private MembershipPlanService _service = null!;
    private Faker _faker = null!;

    [SetUp]
    public void SetUp()
    {
        _mockDb = new Mock<IDbConnection>();
        _service = new MembershipPlanService(_mockDb.Object, Mock.Of<ILogger<MembershipPlanService>>());
        _faker = new Faker();
    }

    // ---------------------------------------------------------------
    // Constructor validation
    // ---------------------------------------------------------------

    [Test]
    public void Constructor_NullDbConnection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MembershipPlanService(null!, Mock.Of<ILogger<MembershipPlanService>>()));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MembershipPlanService(_mockDb.Object, null!));
    }

    [Test]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        Assert.DoesNotThrow(
            () => new MembershipPlanService(_mockDb.Object, Mock.Of<ILogger<MembershipPlanService>>()));
    }

    // ---------------------------------------------------------------
    // GetByIdAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetByIdAsync_DefaultGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetByIdAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // GetAllAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetAllAsync_DefaultSiteId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetAllAsync(Guid.Empty));
    }

    [Test]
    public void GetAllAsync_DefaultSiteId_ActiveOnlyFalse_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetAllAsync(Guid.Empty, activeOnly: false));
    }

    // ---------------------------------------------------------------
    // GetBySlugAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetBySlugAsync_DefaultSiteId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetBySlugAsync(Guid.Empty, "test-slug"));
    }

    [Test]
    public void GetBySlugAsync_NullSlug_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetBySlugAsync(Guid.NewGuid(), null!));
    }

    [Test]
    public void GetBySlugAsync_EmptySlug_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetBySlugAsync(Guid.NewGuid(), ""));
    }

    [Test]
    public void GetBySlugAsync_WhitespaceSlug_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetBySlugAsync(Guid.NewGuid(), "   "));
    }

    // ---------------------------------------------------------------
    // CreateAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void CreateAsync_NullPlan_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.CreateAsync(null!));
    }

    // ---------------------------------------------------------------
    // UpdateAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void UpdateAsync_NullPlan_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.UpdateAsync(null!));
    }

    // ---------------------------------------------------------------
    // DeleteAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void DeleteAsync_DefaultGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.DeleteAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // Interface implementation
    // ---------------------------------------------------------------

    [Test]
    public void Service_ImplementsIMembershipPlanService()
    {
        Assert.That(_service, Is.InstanceOf<IMembershipPlanService>());
    }

    // ---------------------------------------------------------------
    // Model creation
    // ---------------------------------------------------------------

    [Test]
    public void MembershipPlan_CanBeCreated_WithDefaults()
    {
        var plan = new MembershipPlan();

        Assert.That(plan.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(plan.Name, Is.EqualTo(string.Empty));
        Assert.That(plan.Slug, Is.EqualTo(string.Empty));
        Assert.That(plan.Currency, Is.EqualTo("usd"));
        Assert.That(plan.BillingInterval, Is.EqualTo("monthly"));
        Assert.That(plan.IsActive, Is.True);
    }

    [Test]
    public void MembershipPlan_MultipleInstances_AreIndependent()
    {
        var plan1 = new MembershipPlan
        {
            Name = _faker.Commerce.ProductName(),
            Slug = _faker.Lorem.Slug(),
            Price = _faker.Finance.Amount(1, 100)
        };

        var plan2 = new MembershipPlan
        {
            Name = _faker.Commerce.ProductName(),
            Slug = _faker.Lorem.Slug(),
            Price = _faker.Finance.Amount(1, 100)
        };

        Assert.That(plan1.Id, Is.Not.EqualTo(plan2.Id));
        Assert.That(plan1.Name, Is.Not.EqualTo(plan2.Name).Or.Not.EqualTo(plan2.Slug));
    }
}
