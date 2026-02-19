using System.Data;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for managing threaded comments with CRUD operations,
/// moderation workflow (approve, spam, trash), and batch actions.
/// </summary>
public class CommentService : ICommentService
{
    private readonly IDbConnection _db;
    private readonly ILogger<CommentService> _logger;
    private const int MaxDepth = 3;

    /// <summary>
    /// Initializes a new instance of <see cref="CommentService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="logger">The logger.</param>
    public CommentService(IDbConnection db, ILogger<CommentService> logger)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task<Comment?> GetByIdAsync(Guid id)
    {
        Guard.Against.Default(id);
        return await _db.GetAsync<Comment>(id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Comment>> GetByPostAsync(Guid postId, string? status = null,
        int page = 1, int pageSize = 50)
    {
        Guard.Against.Default(postId);

        var offset = (Math.Max(page, 1) - 1) * pageSize;

        if (!string.IsNullOrWhiteSpace(status))
        {
            return await _db.QueryAsync<Comment>(
                "SELECT * FROM comments WHERE post_id = @PostId AND status = @Status ORDER BY created_at LIMIT @Limit OFFSET @Offset",
                new { PostId = postId, Status = status, Limit = pageSize, Offset = offset });
        }

        return await _db.QueryAsync<Comment>(
            "SELECT * FROM comments WHERE post_id = @PostId ORDER BY created_at LIMIT @Limit OFFSET @Offset",
            new { PostId = postId, Limit = pageSize, Offset = offset });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Comment>> GetThreadedByPostAsync(Guid postId)
    {
        Guard.Against.Default(postId);

        // Load all comments for the post, ordered by creation date
        var all = (await _db.QueryAsync<Comment>(
            "SELECT * FROM comments WHERE post_id = @PostId ORDER BY created_at",
            new { PostId = postId })).ToList();

        // Return the flat list; tree assembly is handled by the caller or a DTO mapper.
        // The depth field on each comment indicates its nesting level.
        return all;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Comment>> GetModerationQueueAsync(Guid siteId, string? status = null,
        int page = 1, int pageSize = 50)
    {
        Guard.Against.Default(siteId);

        var offset = (Math.Max(page, 1) - 1) * pageSize;

        if (!string.IsNullOrWhiteSpace(status))
        {
            return await _db.QueryAsync<Comment>(
                @"SELECT c.* FROM comments c
                  INNER JOIN posts p ON p.id = c.post_id
                  WHERE p.site_id = @SiteId AND c.status = @Status
                  ORDER BY c.created_at DESC
                  LIMIT @Limit OFFSET @Offset",
                new { SiteId = siteId, Status = status, Limit = pageSize, Offset = offset });
        }

        return await _db.QueryAsync<Comment>(
            @"SELECT c.* FROM comments c
              INNER JOIN posts p ON p.id = c.post_id
              WHERE p.site_id = @SiteId
              ORDER BY c.created_at DESC
              LIMIT @Limit OFFSET @Offset",
            new { SiteId = siteId, Limit = pageSize, Offset = offset });
    }

    /// <inheritdoc />
    public async Task<Comment> CreateAsync(Comment comment)
    {
        Guard.Against.Null(comment);
        Guard.Against.Default(comment.PostId);
        Guard.Against.NullOrWhiteSpace(comment.AuthorName);
        Guard.Against.NullOrWhiteSpace(comment.BodyMarkdown);

        comment.Id = Guid.NewGuid();
        comment.CreatedAt = DateTime.UtcNow;
        comment.UpdatedAt = DateTime.UtcNow;

        // Calculate depth from parent
        if (comment.ParentId.HasValue && comment.ParentId.Value != Guid.Empty)
        {
            var parent = await _db.GetAsync<Comment>(comment.ParentId.Value);
            comment.Depth = parent != null ? Math.Min(parent.Depth + 1, MaxDepth) : 0;
        }
        else
        {
            comment.Depth = 0;
            comment.ParentId = null;
        }

        if (string.IsNullOrWhiteSpace(comment.Status))
            comment.Status = "pending";

        await _db.InsertAsync(comment);
        return comment;
    }

    /// <inheritdoc />
    public async Task<Comment> UpdateAsync(Comment comment)
    {
        Guard.Against.Null(comment);
        Guard.Against.Default(comment.Id);

        comment.UpdatedAt = DateTime.UtcNow;
        await _db.UpdateAsync(comment);
        return comment;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        Guard.Against.Default(id);

        // Delete child replies first
        await _db.ExecuteAsync(
            "DELETE FROM comments WHERE parent_id = @Id",
            new { Id = id });

        var comment = await _db.GetAsync<Comment>(id);
        if (comment != null)
            await _db.DeleteAsync(comment);
    }

    /// <inheritdoc />
    public async Task ApproveAsync(Guid id)
    {
        Guard.Against.Default(id);
        await UpdateStatusAsync(id, "approved");
    }

    /// <inheritdoc />
    public async Task MarkSpamAsync(Guid id)
    {
        Guard.Against.Default(id);
        await UpdateStatusAsync(id, "spam");
    }

    /// <inheritdoc />
    public async Task TrashAsync(Guid id)
    {
        Guard.Against.Default(id);
        await UpdateStatusAsync(id, "trash");
    }

    /// <inheritdoc />
    public async Task<int> BatchApproveAsync(IEnumerable<Guid> ids)
    {
        Guard.Against.Null(ids);
        return await BatchUpdateStatusAsync(ids, "approved");
    }

    /// <inheritdoc />
    public async Task<int> BatchMarkSpamAsync(IEnumerable<Guid> ids)
    {
        Guard.Against.Null(ids);
        return await BatchUpdateStatusAsync(ids, "spam");
    }

    /// <inheritdoc />
    public async Task<int> BatchTrashAsync(IEnumerable<Guid> ids)
    {
        Guard.Against.Null(ids);
        return await BatchUpdateStatusAsync(ids, "trash");
    }

    /// <inheritdoc />
    public async Task<int> BatchDeleteAsync(IEnumerable<Guid> ids)
    {
        Guard.Against.Null(ids);

        var idList = ids.Where(id => id != Guid.Empty).ToList();
        if (idList.Count == 0) return 0;

        // Delete child replies first
        await _db.ExecuteAsync(
            "DELETE FROM comments WHERE parent_id = ANY(@Ids)",
            new { Ids = idList.ToArray() });

        return await _db.ExecuteAsync(
            "DELETE FROM comments WHERE id = ANY(@Ids)",
            new { Ids = idList.ToArray() });
    }

    /// <inheritdoc />
    public async Task<int> GetCountByPostAsync(Guid postId, string? status = null)
    {
        Guard.Against.Default(postId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            return await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM comments WHERE post_id = @PostId AND status = @Status",
                new { PostId = postId, Status = status });
        }

        return await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM comments WHERE post_id = @PostId",
            new { PostId = postId });
    }

    /// <inheritdoc />
    public async Task<int> GetCountBySiteAsync(Guid siteId, string? status = null)
    {
        Guard.Against.Default(siteId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            return await _db.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM comments c
                  INNER JOIN posts p ON p.id = c.post_id
                  WHERE p.site_id = @SiteId AND c.status = @Status",
                new { SiteId = siteId, Status = status });
        }

        return await _db.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM comments c
              INNER JOIN posts p ON p.id = c.post_id
              WHERE p.site_id = @SiteId",
            new { SiteId = siteId });
    }

    /// <summary>
    /// Updates the status of a single comment.
    /// </summary>
    private async Task UpdateStatusAsync(Guid id, string status)
    {
        await _db.ExecuteAsync(
            "UPDATE comments SET status = @Status, updated_at = @Now WHERE id = @Id",
            new { Status = status, Now = DateTime.UtcNow, Id = id });
    }

    /// <summary>
    /// Updates the status of multiple comments in a single batch.
    /// </summary>
    private async Task<int> BatchUpdateStatusAsync(IEnumerable<Guid> ids, string status)
    {
        var idList = ids.Where(id => id != Guid.Empty).ToList();
        if (idList.Count == 0) return 0;

        return await _db.ExecuteAsync(
            "UPDATE comments SET status = @Status, updated_at = @Now WHERE id = ANY(@Ids)",
            new { Status = status, Now = DateTime.UtcNow, Ids = idList.ToArray() });
    }
}
