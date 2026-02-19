using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing threaded comments on posts.
/// Handles CRUD operations, threaded loading, moderation workflow, and batch actions.
/// </summary>
public interface ICommentService
{
    /// <summary>
    /// Retrieves a comment by its unique identifier.
    /// </summary>
    /// <param name="id">The comment identifier.</param>
    /// <returns>The comment if found; otherwise null.</returns>
    Task<Comment?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves all comments for a post with optional status filtering and pagination.
    /// </summary>
    /// <param name="postId">The post identifier.</param>
    /// <param name="status">Optional status filter (pending, approved, spam, trash).</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A paginated collection of comments.</returns>
    Task<IEnumerable<Comment>> GetByPostAsync(Guid postId, string? status = null, int page = 1, int pageSize = 50);

    /// <summary>
    /// Retrieves comments for a post arranged as a threaded tree.
    /// Top-level comments are returned with replies nested under their parents,
    /// respecting the maximum nesting depth of 3.
    /// </summary>
    /// <param name="postId">The post identifier.</param>
    /// <returns>A collection of top-level comments with nested replies.</returns>
    Task<IEnumerable<Comment>> GetThreadedByPostAsync(Guid postId);

    /// <summary>
    /// Retrieves all comments across a site for the admin moderation queue,
    /// filtered by status with pagination.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="status">Optional status filter (pending, approved, spam, trash).</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A paginated collection of comments for the moderation queue.</returns>
    Task<IEnumerable<Comment>> GetModerationQueueAsync(Guid siteId, string? status = null, int page = 1, int pageSize = 50);

    /// <summary>
    /// Creates a new comment on a post. The depth is automatically calculated
    /// based on the parent comment chain. Maximum depth is 3.
    /// </summary>
    /// <param name="comment">The comment to create.</param>
    /// <returns>The created comment with generated identifier and computed depth.</returns>
    Task<Comment> CreateAsync(Comment comment);

    /// <summary>
    /// Updates an existing comment's body content.
    /// </summary>
    /// <param name="comment">The comment with updated fields.</param>
    /// <returns>The updated comment.</returns>
    Task<Comment> UpdateAsync(Comment comment);

    /// <summary>
    /// Deletes a comment. Replies to the deleted comment are also removed.
    /// </summary>
    /// <param name="id">The comment identifier.</param>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Approves a pending comment, making it visible to visitors.
    /// </summary>
    /// <param name="id">The comment identifier.</param>
    Task ApproveAsync(Guid id);

    /// <summary>
    /// Marks a comment as spam.
    /// </summary>
    /// <param name="id">The comment identifier.</param>
    Task MarkSpamAsync(Guid id);

    /// <summary>
    /// Moves a comment to the trash.
    /// </summary>
    /// <param name="id">The comment identifier.</param>
    Task TrashAsync(Guid id);

    /// <summary>
    /// Approves multiple comments in a single batch operation.
    /// </summary>
    /// <param name="ids">The collection of comment identifiers to approve.</param>
    /// <returns>The number of comments approved.</returns>
    Task<int> BatchApproveAsync(IEnumerable<Guid> ids);

    /// <summary>
    /// Marks multiple comments as spam in a single batch operation.
    /// </summary>
    /// <param name="ids">The collection of comment identifiers to mark as spam.</param>
    /// <returns>The number of comments marked as spam.</returns>
    Task<int> BatchMarkSpamAsync(IEnumerable<Guid> ids);

    /// <summary>
    /// Moves multiple comments to trash in a single batch operation.
    /// </summary>
    /// <param name="ids">The collection of comment identifiers to trash.</param>
    /// <returns>The number of comments trashed.</returns>
    Task<int> BatchTrashAsync(IEnumerable<Guid> ids);

    /// <summary>
    /// Permanently deletes multiple comments in a single batch operation.
    /// </summary>
    /// <param name="ids">The collection of comment identifiers to permanently delete.</param>
    /// <returns>The number of comments permanently deleted.</returns>
    Task<int> BatchDeleteAsync(IEnumerable<Guid> ids);

    /// <summary>
    /// Returns the count of comments for a post, optionally filtered by status.
    /// </summary>
    /// <param name="postId">The post identifier.</param>
    /// <param name="status">Optional status filter.</param>
    /// <returns>The total count of matching comments.</returns>
    Task<int> GetCountByPostAsync(Guid postId, string? status = null);

    /// <summary>
    /// Returns the count of comments across a site, optionally filtered by status.
    /// Used for moderation queue badge counts.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="status">Optional status filter.</param>
    /// <returns>The total count of matching comments.</returns>
    Task<int> GetCountBySiteAsync(Guid siteId, string? status = null);
}
