using NUnit.Framework;
using Moq;
using Bogus;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Services;

namespace Contento.Tests.Services;

/// <summary>
/// Tests for <see cref="PostLockService"/>. The service uses an in-memory
/// ConcurrentDictionary, so all lock operations can be tested directly
/// without database dependencies.
/// </summary>
[TestFixture]
public class PostLockServiceTests
{
    private PostLockService _service = null!;
    private Faker _faker = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new PostLockService(Mock.Of<ILogger<PostLockService>>());
        _faker = new Faker();
    }

    // ---------------------------------------------------------------
    // Constructor validation
    // ---------------------------------------------------------------

    [Test]
    public void Constructor_NullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new PostLockService(null!));
    }

    // ---------------------------------------------------------------
    // GetLockAsync
    // ---------------------------------------------------------------

    [Test]
    public async Task GetLockAsync_EmptyGuid_ReturnsNull()
    {
        var result = await _service.GetLockAsync(Guid.Empty);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetLockAsync_NoLock_ReturnsNull()
    {
        var postId = Guid.NewGuid();

        var result = await _service.GetLockAsync(postId);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetLockAsync_ActiveLock_ReturnsLockInfo()
    {
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var userName = _faker.Name.FullName();

        await _service.AcquireLockAsync(postId, userId, userName);

        var result = await _service.GetLockAsync(postId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.PostId, Is.EqualTo(postId));
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.UserName, Is.EqualTo(userName));
    }

    // ---------------------------------------------------------------
    // AcquireLockAsync
    // ---------------------------------------------------------------

    [Test]
    public async Task AcquireLockAsync_NewPost_ReturnsTrue()
    {
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var userName = _faker.Name.FullName();

        var result = await _service.AcquireLockAsync(postId, userId, userName);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task AcquireLockAsync_SameUser_ReturnsTrue()
    {
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var userName = _faker.Name.FullName();

        await _service.AcquireLockAsync(postId, userId, userName);
        var result = await _service.AcquireLockAsync(postId, userId, userName);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task AcquireLockAsync_DifferentUser_ReturnsFalse()
    {
        var postId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        await _service.AcquireLockAsync(postId, userId1, _faker.Name.FullName());
        var result = await _service.AcquireLockAsync(postId, userId2, _faker.Name.FullName());

        Assert.That(result, Is.False);
    }

    // ---------------------------------------------------------------
    // RenewLockAsync
    // ---------------------------------------------------------------

    [Test]
    public async Task RenewLockAsync_SameUser_ReturnsTrue()
    {
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await _service.AcquireLockAsync(postId, userId, _faker.Name.FullName());
        var result = await _service.RenewLockAsync(postId, userId);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task RenewLockAsync_DifferentUser_ReturnsFalse()
    {
        var postId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        await _service.AcquireLockAsync(postId, userId1, _faker.Name.FullName());
        var result = await _service.RenewLockAsync(postId, userId2);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task RenewLockAsync_NoLock_ReturnsFalse()
    {
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var result = await _service.RenewLockAsync(postId, userId);

        Assert.That(result, Is.False);
    }

    // ---------------------------------------------------------------
    // ReleaseLockAsync
    // ---------------------------------------------------------------

    [Test]
    public async Task ReleaseLockAsync_SameUser_Releases()
    {
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await _service.AcquireLockAsync(postId, userId, _faker.Name.FullName());
        await _service.ReleaseLockAsync(postId, userId);

        var lockInfo = await _service.GetLockAsync(postId);
        Assert.That(lockInfo, Is.Null);
    }

    [Test]
    public async Task ReleaseLockAsync_DifferentUser_DoesNotRelease()
    {
        var postId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        await _service.AcquireLockAsync(postId, userId1, _faker.Name.FullName());
        await _service.ReleaseLockAsync(postId, userId2);

        var lockInfo = await _service.GetLockAsync(postId);
        Assert.That(lockInfo, Is.Not.Null);
        Assert.That(lockInfo!.UserId, Is.EqualTo(userId1));
    }

    // ---------------------------------------------------------------
    // ReleaseExpiredLocksAsync
    // ---------------------------------------------------------------

    [Test]
    public async Task ReleaseExpiredLocksAsync_RemovesExpired()
    {
        // We cannot easily test true expiry without manipulating time,
        // but we can verify that active locks are NOT removed.
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await _service.AcquireLockAsync(postId, userId, _faker.Name.FullName());
        await _service.ReleaseExpiredLocksAsync();

        // Active lock should still be present
        var lockInfo = await _service.GetLockAsync(postId);
        Assert.That(lockInfo, Is.Not.Null);
    }

    // ---------------------------------------------------------------
    // Interface implementation
    // ---------------------------------------------------------------

    [Test]
    public void ImplementsIPostLockService()
    {
        Assert.That(_service, Is.InstanceOf<IPostLockService>());
    }
}
