namespace Contento.Core.Interfaces;

/// <summary>
/// Service for Redis-based caching. All cache operations use the key patterns
/// defined in the Contento cache strategy (e.g., "site:{siteId}:post:{slug}").
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Retrieves a cached value by its key.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the cached value to.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <returns>The cached value if found; otherwise the default value for the type.</returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Retrieves a cached string value by its key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>The cached string if found; otherwise null.</returns>
    Task<string?> GetStringAsync(string key);

    /// <summary>
    /// Sets a value in the cache with an optional expiration time.
    /// The value is serialized to JSON for storage.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">Optional expiration duration. If null, the key does not expire.</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Sets a string value in the cache with an optional expiration time.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The string value to cache.</param>
    /// <param name="expiration">Optional expiration duration. If null, the key does not expire.</param>
    Task SetStringAsync(string key, string value, TimeSpan? expiration = null);

    /// <summary>
    /// Removes a single key from the cache.
    /// </summary>
    /// <param name="key">The cache key to invalidate.</param>
    /// <returns>True if the key existed and was removed; false if it did not exist.</returns>
    Task<bool> InvalidateAsync(string key);

    /// <summary>
    /// Removes all keys matching a pattern from the cache.
    /// Supports Redis glob-style patterns (e.g., "site:*:post:*").
    /// </summary>
    /// <param name="pattern">The key pattern to match for invalidation.</param>
    /// <returns>The number of keys invalidated.</returns>
    Task<long> InvalidateByPatternAsync(string pattern);

    /// <summary>
    /// Checks whether a key exists in the cache.
    /// </summary>
    /// <param name="key">The cache key to check.</param>
    /// <returns>True if the key exists; otherwise false.</returns>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Retrieves a value from the cache, or creates it using the provided factory
    /// if it does not exist, then caches and returns the result.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">The async factory function to create the value if not cached.</param>
    /// <param name="expiration">Optional expiration duration for the cached value.</param>
    /// <returns>The cached or newly created value.</returns>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);

    /// <summary>
    /// Retrieves the remaining time-to-live for a cached key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>The remaining TTL if the key exists and has an expiration; null if the key does not exist or has no expiration.</returns>
    Task<TimeSpan?> GetTimeToLiveAsync(string key);
}
