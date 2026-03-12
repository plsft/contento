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
/// Tests for <see cref="SubscriptionService"/>. The service depends on <see cref="IDbConnection"/>,
/// <see cref="IConfiguration"/>, and <see cref="ILogger{T}"/>. Since most methods use the Stripe
/// SDK directly (SessionService, SubscriptionService, etc.), these tests focus on:
///   - Constructor guard clauses
///   - Verifying the service can be instantiated with valid dependencies
///
/// Methods that exercise Stripe or database calls would require an integration test setup.
/// </summary>
[TestFixture]
public class SubscriptionServiceTests
{
    private Mock<IDbConnection> _mockDb = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Mock<IMembershipPlanService> _mockMembershipPlanService = null!;
    private SubscriptionService _service = null!;
    private Faker _faker = null!;

    [SetUp]
    public void SetUp()
    {
        _mockDb = new Mock<IDbConnection>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockMembershipPlanService = new Mock<IMembershipPlanService>();
        _service = new SubscriptionService(
            _mockDb.Object,
            _mockConfiguration.Object,
            Mock.Of<ILogger<SubscriptionService>>(),
            _mockMembershipPlanService.Object);
        _faker = new Faker();
    }

    // ---------------------------------------------------------------
    // Constructor validation
    // ---------------------------------------------------------------

    [Test]
    public void Constructor_NullDbConnection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new SubscriptionService(
                null!,
                _mockConfiguration.Object,
                Mock.Of<ILogger<SubscriptionService>>(),
                _mockMembershipPlanService.Object));
    }

    [Test]
    public void Constructor_NullConfiguration_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new SubscriptionService(
                _mockDb.Object,
                null!,
                Mock.Of<ILogger<SubscriptionService>>(),
                _mockMembershipPlanService.Object));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new SubscriptionService(
                _mockDb.Object,
                _mockConfiguration.Object,
                null!,
                _mockMembershipPlanService.Object));
    }

    [Test]
    public void Constructor_NullMembershipPlanService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new SubscriptionService(
                _mockDb.Object,
                _mockConfiguration.Object,
                Mock.Of<ILogger<SubscriptionService>>(),
                null!));
    }

    [Test]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        Assert.DoesNotThrow(
            () => new SubscriptionService(
                _mockDb.Object,
                _mockConfiguration.Object,
                Mock.Of<ILogger<SubscriptionService>>(),
                _mockMembershipPlanService.Object));
    }

    // ---------------------------------------------------------------
    // UpdateSubscriptionStatusAsync — requires database (integration test)
    // These methods now perform actual database lookups via
    // QueryFirstOrDefaultAsync, so they cannot be unit tested with
    // a bare Mock<IDbConnection>. Covered by integration tests.
    // ---------------------------------------------------------------

    // ---------------------------------------------------------------
    // HandlePaymentFailureAsync — requires database (integration test)
    // These methods now perform actual database lookups and updates,
    // so they cannot be unit tested with a bare Mock<IDbConnection>.
    // Covered by integration tests.
    // ---------------------------------------------------------------
}
