using System.Text.Json;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;

namespace Contento.Services;

/// <summary>
/// Redis-backed cache service using StackExchange.Redis with JSON serialization.
/// Falls back gracefully when Redis is unavailable — all operations become no-ops.
/// </summary>
public class CacheService : ICacheService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly IDatabase? _cache;
    private readonly ILogger<CacheService> _logger;
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(15);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public CacheService(IConnectionMultiplexer? redis, ILogger<CacheService> logger)
    {
        _redis = redis;
        _cache = redis?.GetDatabase();
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key)
    {
        if (_cache == null) return default;

        var value = await _cache.StringGetAsync(key);
        if (value.IsNullOrEmpty)
            return default;

        return JsonSerializer.Deserialize<T>(value.ToString(), JsonOptions);
    }

    /// <inheritdoc />
    public async Task<string?> GetStringAsync(string key)
    {
        if (_cache == null) return null;

        var value = await _cache.StringGetAsync(key);
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (_cache == null) return;

        var json = JsonSerializer.Serialize(value, JsonOptions);
        await _cache.StringSetAsync(key, json, expiration ?? DefaultExpiry);
    }

    /// <inheritdoc />
    public async Task SetStringAsync(string key, string value, TimeSpan? expiration = null)
    {
        if (_cache == null) return;

        await _cache.StringSetAsync(key, value, expiration ?? DefaultExpiry);
    }

    /// <inheritdoc />
    public async Task<bool> InvalidateAsync(string key)
    {
        if (_cache == null) return false;

        return await _cache.KeyDeleteAsync(key);
    }

    /// <inheritdoc />
    public async Task<long> InvalidateByPatternAsync(string pattern)
    {
        if (_redis == null || _cache == null) return 0;

        long count = 0;
        var server = _redis.GetServer(_redis.GetEndPoints().First());

        await foreach (var key in server.KeysAsync(pattern: pattern))
        {
            await _cache.KeyDeleteAsync(key);
            count++;
        }

        return count;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key)
    {
        if (_cache == null) return false;

        return await _cache.KeyExistsAsync(key);
    }

    /// <inheritdoc />
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        if (_cache != null)
        {
            var cached = await GetAsync<T>(key);
            if (cached != null)
                return cached;
        }

        var value = await factory();

        if (_cache != null && value != null)
            await SetAsync(key, value, expiration);

        return value;
    }

    /// <inheritdoc />
    public async Task<TimeSpan?> GetTimeToLiveAsync(string key)
    {
        if (_cache == null) return null;

        return await _cache.KeyTimeToLiveAsync(key);
    }
}
