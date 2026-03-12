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
/// Tests for <see cref="PostTypeService"/>. The service depends on <see cref="IDbConnection"/>
/// and <see cref="ILogger{PostTypeService}"/>. Since Tuxedo extension methods (QueryAsync,
/// InsertAsync, etc.) on IDbConnection cannot be easily mocked, these tests focus on:
///   - Constructor guard clauses
///   - Method-level argument validation (Guard.Against.*)
///   - Verifying the service can be instantiated with valid dependencies
///
/// Methods that exercise the full database path would require an integration test setup.
/// </summary>
[TestFixture]
public class PostTypeServiceTests
{
    private Mock<IDbConnection> _mockDb = null!;
    private PostTypeService _service = null!;
    private Faker _faker = null!;

    [SetUp]
    public void SetUp()
    {
        _mockDb = new Mock<IDbConnection>();
        _service = new PostTypeService(_mockDb.Object, Mock.Of<ILogger<PostTypeService>>());
        _faker = new Faker();
    }

    // ---------------------------------------------------------------
    // Constructor validation
    // ---------------------------------------------------------------

    [Test]
    public void Constructor_NullDbConnection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new PostTypeService(null!, Mock.Of<ILogger<PostTypeService>>()));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new PostTypeService(_mockDb.Object, null!));
    }

    [Test]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        Assert.DoesNotThrow(
            () => new PostTypeService(_mockDb.Object, Mock.Of<ILogger<PostTypeService>>()));
    }

    // ---------------------------------------------------------------
    // GetByIdAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetByIdAsync_EmptyGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetByIdAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // GetBySlugAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetBySlugAsync_EmptySiteId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetBySlugAsync(Guid.Empty, "some-slug"));
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
    // GetAllAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetAllAsync_EmptySiteId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetAllAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // CreateAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void CreateAsync_NullPostType_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.CreateAsync(null!));
    }

    [Test]
    public void CreateAsync_NullName_ThrowsArgumentException()
    {
        var postType = CreateValidPostType();
        postType.Name = null!;

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(postType));
    }

    [Test]
    public void CreateAsync_EmptyName_ThrowsArgumentException()
    {
        var postType = CreateValidPostType();
        postType.Name = "";

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(postType));
    }

    [Test]
    public void CreateAsync_NullSlug_ThrowsArgumentException()
    {
        var postType = CreateValidPostType();
        postType.Slug = null!;

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(postType));
    }

    [Test]
    public void CreateAsync_EmptySlug_ThrowsArgumentException()
    {
        var postType = CreateValidPostType();
        postType.Slug = "";

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(postType));
    }

    // ---------------------------------------------------------------
    // UpdateAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void UpdateAsync_NullPostType_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.UpdateAsync(null!));
    }

    [Test]
    public void UpdateAsync_DefaultPostTypeId_ThrowsArgumentException()
    {
        var postType = CreateValidPostType();
        postType.Id = Guid.Empty;

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.UpdateAsync(postType));
    }

    // ---------------------------------------------------------------
    // DeleteAsync -- argument validation
    // ---------------------------------------------------------------

    [Test]
    public void DeleteAsync_EmptyGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.DeleteAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // Interface implementation
    // ---------------------------------------------------------------

    [Test]
    public void Service_ImplementsIPostTypeService()
    {
        Assert.That(_service, Is.InstanceOf<IPostTypeService>());
    }

    // ---------------------------------------------------------------
    // Multiple instances
    // ---------------------------------------------------------------

    [Test]
    public void MultipleInstances_CanBeCreatedIndependently()
    {
        var db1 = new Mock<IDbConnection>();
        var db2 = new Mock<IDbConnection>();

        var service1 = new PostTypeService(db1.Object, Mock.Of<ILogger<PostTypeService>>());
        var service2 = new PostTypeService(db2.Object, Mock.Of<ILogger<PostTypeService>>());

        Assert.That(service1, Is.Not.SameAs(service2));
    }

    // ---------------------------------------------------------------
    // PostType model defaults
    // ---------------------------------------------------------------

    [Test]
    public void PostType_DefaultValues_AreCorrect()
    {
        var postType = new PostType();

        Assert.That(postType.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(postType.Name, Is.EqualTo(string.Empty));
        Assert.That(postType.Slug, Is.EqualTo(string.Empty));
        Assert.That(postType.Fields, Is.EqualTo("[]"));
        Assert.That(postType.Settings, Is.EqualTo("{}"));
        Assert.That(postType.IsSystem, Is.False);
        Assert.That(postType.SortOrder, Is.EqualTo(0));
        Assert.That(postType.Icon, Is.Null);
    }

    [Test]
    public void PostType_FieldsAreSettable()
    {
        var postType = new PostType
        {
            Id = Guid.NewGuid(),
            SiteId = Guid.NewGuid(),
            Name = _faker.Commerce.Department(),
            Slug = _faker.Internet.DomainWord(),
            Icon = "pencil",
            Fields = "[{\"name\":\"body\",\"type\":\"richtext\"}]",
            Settings = "{\"allowComments\":true}",
            IsSystem = true,
            SortOrder = 5
        };

        Assert.That(postType.Name, Is.Not.Empty);
        Assert.That(postType.Slug, Is.Not.Empty);
        Assert.That(postType.Icon, Is.EqualTo("pencil"));
        Assert.That(postType.IsSystem, Is.True);
        Assert.That(postType.SortOrder, Is.EqualTo(5));
    }

    // ---------------------------------------------------------------
    // Bogus-generated data for fuzz-style guard validation
    // ---------------------------------------------------------------

    [Test]
    public void CreateAsync_BogusPostType_WithValidFields_PassesGuards()
    {
        var postType = new PostType
        {
            SiteId = Guid.NewGuid(),
            Name = _faker.Commerce.Department(),
            Slug = _faker.Internet.DomainWord()
        };

        // The guard clauses should not throw for valid input. The actual InsertAsync
        // will throw because IDbConnection is a mock without Tuxedo extension wiring,
        // so we expect a different exception type.
        var ex = Assert.CatchAsync(async () => await _service.CreateAsync(postType));

        // If an exception occurs, it should NOT be ArgumentNullException or ArgumentException
        // (those would indicate a guard failure)
        if (ex != null)
        {
            Assert.That(ex, Is.Not.TypeOf<ArgumentNullException>());
            Assert.That(ex, Is.Not.TypeOf<ArgumentException>());
        }
    }

    [Test]
    public void UpdateAsync_BogusPostType_WithValidFields_PassesGuards()
    {
        var postType = new PostType
        {
            Id = Guid.NewGuid(),
            SiteId = Guid.NewGuid(),
            Name = _faker.Commerce.Department(),
            Slug = _faker.Internet.DomainWord()
        };

        var ex = Assert.CatchAsync(async () => await _service.UpdateAsync(postType));

        if (ex != null)
        {
            Assert.That(ex, Is.Not.TypeOf<ArgumentNullException>());
            Assert.That(ex, Is.Not.TypeOf<ArgumentException>());
        }
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private PostType CreateValidPostType()
    {
        return new PostType
        {
            SiteId = Guid.NewGuid(),
            Name = "Blog Post",
            Slug = "blog-post"
        };
    }
}
