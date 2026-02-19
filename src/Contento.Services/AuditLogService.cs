using System.Data;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for recording and querying the audit trail of all state-changing
/// actions in the system.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly IDbConnection _db;
    private readonly ILogger<AuditLogService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AuditLogService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="logger">The logger.</param>
    public AuditLogService(IDbConnection db, ILogger<AuditLogService> logger)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task LogActionAsync(Guid? siteId, Guid? userId, string action,
        string? entityType = null, Guid? entityId = null, string? detailsJson = null,
        string? ipAddress = null)
    {
        Guard.Against.NullOrWhiteSpace(action);

        var entry = new AuditLog
        {
            Id = Guid.NewGuid(),
            SiteId = siteId,
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = detailsJson,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        await _db.InsertAsync(entry);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> QueryAsync(Guid? siteId = null, Guid? userId = null,
        string? action = null, string? entityType = null, Guid? entityId = null,
        DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50)
    {
        var (whereClause, parameters) = BuildFilterClause(siteId, userId, action, entityType, entityId, from, to);
        var offset = (Math.Max(page, 1) - 1) * pageSize;

        var sql = $@"SELECT * FROM audit_log
                     WHERE {whereClause}
                     ORDER BY created_at DESC
                     LIMIT @Limit OFFSET @Offset";

        parameters["Limit"] = pageSize;
        parameters["Offset"] = offset;

        return await _db.QueryAsync<AuditLog>(sql, parameters);
    }

    /// <inheritdoc />
    public async Task<int> GetTotalCountAsync(Guid? siteId = null, Guid? userId = null,
        string? action = null, string? entityType = null, Guid? entityId = null,
        DateTime? from = null, DateTime? to = null)
    {
        var (whereClause, parameters) = BuildFilterClause(siteId, userId, action, entityType, entityId, from, to);
        var sql = $"SELECT COUNT(*) FROM audit_log WHERE {whereClause}";
        return await _db.ExecuteScalarAsync<int>(sql, parameters);
    }

    /// <inheritdoc />
    public async Task<AuditLog?> GetByIdAsync(Guid id)
    {
        Guard.Against.Default(id);
        return await _db.GetAsync<AuditLog>(id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, Guid entityId,
        int page = 1, int pageSize = 50)
    {
        Guard.Against.NullOrWhiteSpace(entityType);
        Guard.Against.Default(entityId);

        var offset = (Math.Max(page, 1) - 1) * pageSize;
        return await _db.QueryAsync<AuditLog>(
            @"SELECT * FROM audit_log
              WHERE entity_type = @EntityType AND entity_id = @EntityId
              ORDER BY created_at DESC
              LIMIT @Limit OFFSET @Offset",
            new { EntityType = entityType, EntityId = entityId, Limit = pageSize, Offset = offset });
    }

    /// <summary>
    /// Builds the WHERE clause and parameter dictionary for filtered audit log queries.
    /// </summary>
    private static (string WhereClause, Dictionary<string, object> Parameters) BuildFilterClause(
        Guid? siteId, Guid? userId, string? action, string? entityType, Guid? entityId,
        DateTime? from, DateTime? to)
    {
        var conditions = new List<string> { "TRUE" };
        var parameters = new Dictionary<string, object>();

        if (siteId.HasValue && siteId.Value != Guid.Empty)
        {
            conditions.Add("site_id = @SiteId");
            parameters["SiteId"] = siteId.Value;
        }

        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            conditions.Add("user_id = @UserId");
            parameters["UserId"] = userId.Value;
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            if (action.EndsWith(".*"))
            {
                conditions.Add("action LIKE @ActionPattern");
                parameters["ActionPattern"] = action.TrimEnd('*') + "%";
            }
            else
            {
                conditions.Add("action = @Action");
                parameters["Action"] = action;
            }
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            conditions.Add("entity_type = @EntityType");
            parameters["EntityType"] = entityType;
        }

        if (entityId.HasValue && entityId.Value != Guid.Empty)
        {
            conditions.Add("entity_id = @EntityId");
            parameters["EntityId"] = entityId.Value;
        }

        if (from.HasValue)
        {
            conditions.Add("created_at >= @From");
            parameters["From"] = from.Value;
        }

        if (to.HasValue)
        {
            conditions.Add("created_at <= @To");
            parameters["To"] = to.Value;
        }

        return (string.Join(" AND ", conditions), parameters);
    }
}
