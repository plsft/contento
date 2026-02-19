using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for recording and querying the audit trail of all state-changing actions.
/// Every significant operation in Contento is logged for security and accountability.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Logs an audit action. Called automatically by Guardian middleware and
    /// manually by services for business-level actions.
    /// </summary>
    /// <param name="siteId">The site identifier where the action occurred, or null for global actions.</param>
    /// <param name="userId">The identifier of the user who performed the action, or null for system actions.</param>
    /// <param name="action">The action identifier (e.g., "post.publish", "user.login", "plugin.install").</param>
    /// <param name="entityType">The type of entity affected (e.g., "post", "comment", "user").</param>
    /// <param name="entityId">The identifier of the affected entity, or null.</param>
    /// <param name="detailsJson">Optional JSON string with additional action details.</param>
    /// <param name="ipAddress">The IP address of the request originator.</param>
    Task LogActionAsync(Guid? siteId, Guid? userId, string action, string? entityType = null,
        Guid? entityId = null, string? detailsJson = null, string? ipAddress = null);

    /// <summary>
    /// Queries audit log entries with optional filters and pagination.
    /// Results are ordered by creation date descending (newest first).
    /// </summary>
    /// <param name="siteId">Optional site identifier filter.</param>
    /// <param name="userId">Optional user identifier filter.</param>
    /// <param name="action">Optional action filter (exact match or prefix with wildcard, e.g., "post.*").</param>
    /// <param name="entityType">Optional entity type filter.</param>
    /// <param name="entityId">Optional entity identifier filter.</param>
    /// <param name="from">Optional start date filter (inclusive).</param>
    /// <param name="to">Optional end date filter (inclusive).</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A paginated collection of audit log entries.</returns>
    Task<IEnumerable<AuditLog>> QueryAsync(Guid? siteId = null, Guid? userId = null, string? action = null,
        string? entityType = null, Guid? entityId = null, DateTime? from = null, DateTime? to = null,
        int page = 1, int pageSize = 50);

    /// <summary>
    /// Returns the total count of audit log entries matching the given filters.
    /// </summary>
    /// <param name="siteId">Optional site identifier filter.</param>
    /// <param name="userId">Optional user identifier filter.</param>
    /// <param name="action">Optional action filter.</param>
    /// <param name="entityType">Optional entity type filter.</param>
    /// <param name="entityId">Optional entity identifier filter.</param>
    /// <param name="from">Optional start date filter (inclusive).</param>
    /// <param name="to">Optional end date filter (inclusive).</param>
    /// <returns>The total count of matching audit log entries.</returns>
    Task<int> GetTotalCountAsync(Guid? siteId = null, Guid? userId = null, string? action = null,
        string? entityType = null, Guid? entityId = null, DateTime? from = null, DateTime? to = null);

    /// <summary>
    /// Retrieves a single audit log entry by its identifier.
    /// </summary>
    /// <param name="id">The audit log entry identifier.</param>
    /// <returns>The audit log entry if found; otherwise null.</returns>
    Task<AuditLog?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves all audit log entries for a specific entity, ordered by date descending.
    /// Useful for viewing the history of changes to a particular post, user, etc.
    /// </summary>
    /// <param name="entityType">The entity type (e.g., "post", "user").</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A paginated collection of audit log entries for the entity.</returns>
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, int page = 1, int pageSize = 50);
}
