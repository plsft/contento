namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing concurrent edit locks on posts.
/// Prevents two users from editing the same post simultaneously.
/// </summary>
public interface IPostLockService
{
    /// <summary>
    /// Gets the current lock information for a post, if any.
    /// </summary>
    /// <param name="postId">The post identifier.</param>
    /// <returns>Lock info if the post is locked; otherwise null.</returns>
    Task<PostLockInfo?> GetLockAsync(Guid postId);

    /// <summary>
    /// Attempts to acquire an exclusive edit lock on a post.
    /// </summary>
    /// <param name="postId">The post identifier.</param>
    /// <param name="userId">The user requesting the lock.</param>
    /// <param name="userName">Display name of the user requesting the lock.</param>
    /// <returns>True if the lock was acquired; false if another user holds it.</returns>
    Task<bool> AcquireLockAsync(Guid postId, Guid userId, string userName);

    /// <summary>
    /// Renews an existing lock, extending its expiry. Only succeeds if the same user holds the lock.
    /// </summary>
    /// <param name="postId">The post identifier.</param>
    /// <param name="userId">The user renewing the lock.</param>
    /// <returns>True if renewed; false if the lock is held by another user or does not exist.</returns>
    Task<bool> RenewLockAsync(Guid postId, Guid userId);

    /// <summary>
    /// Releases a lock held by the specified user.
    /// </summary>
    /// <param name="postId">The post identifier.</param>
    /// <param name="userId">The user releasing the lock.</param>
    Task ReleaseLockAsync(Guid postId, Guid userId);

    /// <summary>
    /// Releases all locks that have passed their expiry time. Used by background cleanup.
    /// </summary>
    Task ReleaseExpiredLocksAsync();
}

/// <summary>
/// Information about who holds an edit lock on a post.
/// </summary>
public class PostLockInfo
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime AcquiredAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
