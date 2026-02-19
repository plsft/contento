using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Contento.Services;

namespace Contento.Tests.Services;

/// <summary>
/// Tests for <see cref="CacheService"/>. The service depends on
/// <see cref="IConnectionMultiplexer"/> (nullable). When Redis is unavailable (null),
/// all cache operations should be no-ops returning safe defaults. This test class
/// exercises both the null-Redis fallback path and the wired-up mock path.
/// </summary>
[TestFixture]
public class CacheServiceTests
{
    // ---------------------------------------------------------------
    // Constructor
    // ---------------------------------------------------------------

    [Test]
    public void Constructor_NullRedis_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => new CacheService(null, Mock.Of<ILogger<CacheService>>()));
    }

    [Test]
    public void Constructor_ValidRedis_DoesNotThrow()
    {
        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDb = new Mock<IDatabase>();
        mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDb.Object);

        Assert.DoesNotThrow(() => new CacheService(mockRedis.Object, Mock.Of<ILogger<CacheService>>()));
    }

    // ---------------------------------------------------------------
    // Null Redis — graceful fallback (all methods return defaults)
    // ---------------------------------------------------------------

    [TestFixture]
    public class NullRedisFallback
    {
        private CacheService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _service = new CacheService(null, Mock.Of<ILogger<CacheService>>());
        }

        [Test]
        public async Task GetAsync_ReturnsDefault()
        {
            var result = await _service.GetAsync<string>("any-key");

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetAsync_ValueType_ReturnsDefault()
        {
            var result = await _service.GetAsync<int>("any-key");

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public async Task GetStringAsync_ReturnsNull()
        {
            var result = await _service.GetStringAsync("any-key");

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task SetAsync_DoesNotThrow()
        {
            await _service.SetAsync("key", "value");

            // No exception means the no-op path succeeded
            Assert.Pass();
        }

        [Test]
        public async Task SetAsync_WithExpiration_DoesNotThrow()
        {
            await _service.SetAsync("key", new { Name = "test" }, TimeSpan.FromMinutes(5));

            Assert.Pass();
        }

        [Test]
        public async Task SetStringAsync_DoesNotThrow()
        {
            await _service.SetStringAsync("key", "value");

            Assert.Pass();
        }

        [Test]
        public async Task SetStringAsync_WithExpiration_DoesNotThrow()
        {
            await _service.SetStringAsync("key", "value", TimeSpan.FromMinutes(10));

            Assert.Pass();
        }

        [Test]
        public async Task InvalidateAsync_ReturnsFalse()
        {
            var result = await _service.InvalidateAsync("any-key");

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task InvalidateByPatternAsync_ReturnsZero()
        {
            var result = await _service.InvalidateByPatternAsync("site:*:post:*");

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public async Task ExistsAsync_ReturnsFalse()
        {
            var result = await _service.ExistsAsync("any-key");

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task GetTimeToLiveAsync_ReturnsNull()
        {
            var result = await _service.GetTimeToLiveAsync("any-key");

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetOrSetAsync_CallsFactory_WhenRedisIsNull()
        {
            var factoryCalled = false;
            var result = await _service.GetOrSetAsync("key", async () =>
            {
                factoryCalled = true;
                await Task.CompletedTask;
                return "factory-value";
            });

            Assert.That(factoryCalled, Is.True);
            Assert.That(result, Is.EqualTo("factory-value"));
        }

        [Test]
        public async Task GetOrSetAsync_ReturnsFactoryResult_WhenRedisIsNull()
        {
            var expected = new TestDto { Id = 42, Name = "Test" };

            var result = await _service.GetOrSetAsync("key", () => Task.FromResult(expected));

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(42));
            Assert.That(result.Name, Is.EqualTo("Test"));
        }

        [Test]
        public async Task GetOrSetAsync_WithExpiration_CallsFactory_WhenRedisIsNull()
        {
            var result = await _service.GetOrSetAsync(
                "key",
                () => Task.FromResult(99),
                TimeSpan.FromMinutes(5));

            Assert.That(result, Is.EqualTo(99));
        }
    }

    // ---------------------------------------------------------------
    // Mocked Redis — verify interaction with IDatabase
    // ---------------------------------------------------------------

    [TestFixture]
    public class MockedRedis
    {
        private Mock<IConnectionMultiplexer> _mockRedis = null!;
        private Mock<IDatabase> _mockDatabase = null!;
        private CacheService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _mockRedis = new Mock<IConnectionMultiplexer>();
            _mockDatabase = new Mock<IDatabase>(MockBehavior.Loose);
            _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                       .Returns(_mockDatabase.Object);
            _service = new CacheService(_mockRedis.Object, Mock.Of<ILogger<CacheService>>());
        }

        [Test]
        public async Task GetStringAsync_CallsStringGetAsync()
        {
            _mockDatabase
                .Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisValue.Null);

            var result = await _service.GetStringAsync("test-key");

            Assert.That(result, Is.Null);
            _mockDatabase.Verify(
                db => db.StringGetAsync(
                    It.Is<RedisKey>(k => k == "test-key"),
                    It.IsAny<CommandFlags>()),
                Times.Once);
        }

        [Test]
        public async Task GetStringAsync_ReturnsValue_WhenKeyExists()
        {
            _mockDatabase
                .Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync((RedisValue)"cached-value");

            var result = await _service.GetStringAsync("test-key");

            Assert.That(result, Is.EqualTo("cached-value"));
        }

        [Test]
        public async Task SetStringAsync_CallsStringSetAsync()
        {
            // StackExchange.Redis StringSetAsync has multiple overloads; the one
            // used internally may vary by version. We verify the call was made by
            // checking that no exception is thrown and the mock was interacted with.
            await _service.SetStringAsync("key", "value", TimeSpan.FromMinutes(5));

            // Verify that StringSetAsync was called (any overload)
            Assert.That(_mockDatabase.Invocations.Count, Is.GreaterThan(0));
            var setCall = _mockDatabase.Invocations
                .FirstOrDefault(i => i.Method.Name == "StringSetAsync");
            Assert.That(setCall, Is.Not.Null);
            Assert.That(setCall!.Arguments[0].ToString(), Is.EqualTo("key"));
            Assert.That(setCall.Arguments[1].ToString(), Is.EqualTo("value"));
        }

        [Test]
        public async Task InvalidateAsync_CallsKeyDeleteAsync()
        {
            _mockDatabase
                .Setup(db => db.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);

            var result = await _service.InvalidateAsync("delete-me");

            Assert.That(result, Is.True);
            _mockDatabase.Verify(
                db => db.KeyDeleteAsync(
                    It.Is<RedisKey>(k => k == "delete-me"),
                    It.IsAny<CommandFlags>()),
                Times.Once);
        }

        [Test]
        public async Task InvalidateAsync_ReturnsFalse_WhenKeyDoesNotExist()
        {
            _mockDatabase
                .Setup(db => db.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(false);

            var result = await _service.InvalidateAsync("nonexistent");

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ExistsAsync_CallsKeyExistsAsync()
        {
            _mockDatabase
                .Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);

            var result = await _service.ExistsAsync("check-key");

            Assert.That(result, Is.True);
            _mockDatabase.Verify(
                db => db.KeyExistsAsync(
                    It.Is<RedisKey>(k => k == "check-key"),
                    It.IsAny<CommandFlags>()),
                Times.Once);
        }

        [Test]
        public async Task ExistsAsync_ReturnsFalse_WhenKeyDoesNotExist()
        {
            _mockDatabase
                .Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(false);

            var result = await _service.ExistsAsync("missing-key");

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task GetTimeToLiveAsync_CallsKeyTimeToLiveAsync()
        {
            var expectedTtl = TimeSpan.FromMinutes(10);
            _mockDatabase
                .Setup(db => db.KeyTimeToLiveAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(expectedTtl);

            var result = await _service.GetTimeToLiveAsync("ttl-key");

            Assert.That(result, Is.EqualTo(expectedTtl));
        }

        [Test]
        public async Task GetTimeToLiveAsync_ReturnsNull_WhenNoExpiry()
        {
            _mockDatabase
                .Setup(db => db.KeyTimeToLiveAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync((TimeSpan?)null);

            var result = await _service.GetTimeToLiveAsync("no-expiry-key");

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetAsync_ReturnsDeserializedObject_WhenValueExists()
        {
            var json = "{\"id\":42,\"name\":\"Cached\"}";
            _mockDatabase
                .Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync((RedisValue)json);

            var result = await _service.GetAsync<TestDto>("dto-key");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo(42));
            Assert.That(result.Name, Is.EqualTo("Cached"));
        }

        [Test]
        public async Task GetAsync_ReturnsDefault_WhenValueIsNull()
        {
            _mockDatabase
                .Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisValue.Null);

            var result = await _service.GetAsync<TestDto>("empty-key");

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task SetAsync_SerializesAndStoresObject()
        {
            var dto = new TestDto { Id = 1, Name = "Serialize Me" };
            await _service.SetAsync("dto-key", dto, TimeSpan.FromMinutes(10));

            // Verify StringSetAsync was called with the serialized JSON
            var setCall = _mockDatabase.Invocations
                .FirstOrDefault(i => i.Method.Name == "StringSetAsync");
            Assert.That(setCall, Is.Not.Null);
            Assert.That(setCall!.Arguments[0].ToString(), Is.EqualTo("dto-key"));
            Assert.That(setCall.Arguments[1].ToString(), Does.Contain("Serialize Me"));
        }

        [Test]
        public async Task GetOrSetAsync_ReturnsCachedValue_WhenExists()
        {
            var json = "{\"id\":99,\"name\":\"FromCache\"}";
            _mockDatabase
                .Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync((RedisValue)json);

            var factoryCalled = false;
            var result = await _service.GetOrSetAsync<TestDto>("key", () =>
            {
                factoryCalled = true;
                return Task.FromResult(new TestDto { Id = 1, Name = "FromFactory" });
            });

            Assert.That(factoryCalled, Is.False);
            Assert.That(result.Id, Is.EqualTo(99));
            Assert.That(result.Name, Is.EqualTo("FromCache"));
        }

        [Test]
        public async Task GetOrSetAsync_CallsFactoryAndCaches_WhenNotCached()
        {
            _mockDatabase
                .Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisValue.Null);

            var expected = new TestDto { Id = 55, Name = "Fresh" };
            var result = await _service.GetOrSetAsync("key", () => Task.FromResult(expected));

            Assert.That(result.Id, Is.EqualTo(55));
            Assert.That(result.Name, Is.EqualTo("Fresh"));

            // Verify the value was written to cache (any StringSetAsync overload)
            var setCall = _mockDatabase.Invocations
                .FirstOrDefault(i => i.Method.Name == "StringSetAsync");
            Assert.That(setCall, Is.Not.Null, "StringSetAsync should have been called to cache the factory result");
            Assert.That(setCall!.Arguments[0].ToString(), Is.EqualTo("key"));
            Assert.That(setCall.Arguments[1].ToString(), Does.Contain("Fresh"));
        }
    }

    // ---------------------------------------------------------------
    // Shared test DTO
    // ---------------------------------------------------------------

    private class TestDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
