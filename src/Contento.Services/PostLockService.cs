using System.Collections.Concurrent;
using Noundry.Guardian;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;

namespace Contento.Services;

/// <summary>
/// In-memory post locking service using ConcurrentDictionary.
/// Prevents two users from editing the same post simultaneously.
/// Lock TTL is 2 minutes; locks must be renewed by the editor's heartbeat.
/// </summary>
public class PostLockService : IPostLockService
{
    private readonly ILogger<PostLockService> _logger;

    private static readonly ConcurrentDictionary<Guid, PostLockInfo> _locks = new();
    private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Initializes a new instance of <see cref="PostLockService"/>.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public PostLockService(ILogger<PostLockService> logger)
    {
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public Task<PostLockInfo?> GetLockAsync(Guid postId)
    {
        if (_locks.TryGetValue(postId, out var lockInfo))
        {
            if (lockInfo.ExpiresAt > DateTime.UtcNow)
            {
                return Task.FromResult<PostLockInfo?>(lockInfo);
            }

            // Lock has expired — remove it
            _locks.TryRemove(postId, out _);
            _logger.LogDebug("Removed expired lock for post {PostId}", postId);
        }

        return Task.FromResult<PostLockInfo?>(null);
    }

    /// <inheritdoc />
    public Task<bool> AcquireLockAsync(Guid postId, Guid userId, string userName)
    {
        Guard.Against.NullOrWhiteSpace(userName);

        var now = DateTime.UtcNow;

        if (_locks.TryGetValue(postId, out var existing))
        {
            // If the lock is expired, remove it and proceed
            if (existing.ExpiresAt <= now)
            {
                _locks.TryRemove(postId, out _);
            }
            else if (existing.UserId != userId)
            {
                // Different user holds an active lock
                _logger.LogInformation(
                    "Lock acquisition denied for post {PostId}: held by user {HolderId}, requested by {RequesterId}",
                    postId, existing.UserId, userId);
                return Task.FromResult(false);
            }
        }

        var lockInfo = new PostLockInfo
        {
            PostId = postId,
            UserId = userId,
            UserName = userName,
            AcquiredAt = now,
            ExpiresAt = now.Add(LockTtl)
        };

        _locks[postId] = lockInfo;

        _logger.LogInformation("Lock acquired for post {PostId} by user {UserId} ({UserName})",
            postId, userId, userName);

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> RenewLockAsync(Guid postId, Guid userId)
    {
        if (!_locks.TryGetValue(postId, out var existing))
        {
            return Task.FromResult(false);
        }

        if (existing.ExpiresAt <= DateTime.UtcNow)
        {
            _locks.TryRemove(postId, out _);
            return Task.FromResult(false);
        }

        if (existing.UserId != userId)
        {
            return Task.FromResult(false);
        }

        existing.ExpiresAt = DateTime.UtcNow.Add(LockTtl);

        _logger.LogDebug("Lock renewed for post {PostId} by user {UserId}", postId, userId);

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task ReleaseLockAsync(Guid postId, Guid userId)
    {
        if (_locks.TryGetValue(postId, out var existing) && existing.UserId == userId)
        {
            _locks.TryRemove(postId, out _);
            _logger.LogInformation("Lock released for post {PostId} by user {UserId}", postId, userId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ReleaseExpiredLocksAsync()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _locks
            .Where(kvp => kvp.Value.ExpiresAt <= now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _locks.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogInformation("Released {Count} expired post lock(s)", expiredKeys.Count);
        }

        return Task.CompletedTask;
    }
}
