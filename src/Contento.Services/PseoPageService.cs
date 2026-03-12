using System.Data;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for managing individual pSEO pages — CRUD, status tracking, and bulk operations.
/// </summary>
public class PseoPageService : IPseoPageService
{
    private readonly IDbConnection _db;
    private readonly ILogger<PseoPageService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PseoPageService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="logger">The logger.</param>
    public PseoPageService(IDbConnection db, ILogger<PseoPageService> logger)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task<PseoPage?> GetByIdAsync(Guid id)
    {
        Guard.Against.Default(id);
        return await _db.GetAsync<PseoPage>(id);
    }

    /// <inheritdoc />
    public async Task<List<PseoPage>> GetByCollectionIdAsync(Guid collectionId, string? status, int page, int pageSize)
    {
        Guard.Against.Default(collectionId);

        var conditions = new List<string> { "collection_id = @CollectionId" };
        var parameters = new Dictionary<string, object> { { "CollectionId", collectionId } };

        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("status = @Status");
            parameters["Status"] = status;
        }

        var offset = (Math.Max(page, 1) - 1) * pageSize;
        parameters["Limit"] = pageSize;
        parameters["Offset"] = offset;

        var sql = $@"SELECT * FROM pseo_pages
                     WHERE {string.Join(" AND ", conditions)}
                     ORDER BY created_at DESC
                     LIMIT @Limit OFFSET @Offset";

        var results = await _db.QueryAsync<PseoPage>(sql, parameters);
        return results.ToList();
    }

    /// <inheritdoc />
    public async Task<List<PseoPage>> GetByProjectIdAsync(Guid projectId, string? status, int page, int pageSize)
    {
        Guard.Against.Default(projectId);

        var conditions = new List<string> { "project_id = @ProjectId" };
        var parameters = new Dictionary<string, object> { { "ProjectId", projectId } };

        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("status = @Status");
            parameters["Status"] = status;
        }

        var offset = (Math.Max(page, 1) - 1) * pageSize;
        parameters["Limit"] = pageSize;
        parameters["Offset"] = offset;

        var sql = $@"SELECT * FROM pseo_pages
                     WHERE {string.Join(" AND ", conditions)}
                     ORDER BY created_at DESC
                     LIMIT @Limit OFFSET @Offset";

        var results = await _db.QueryAsync<PseoPage>(sql, parameters);
        return results.ToList();
    }

    /// <inheritdoc />
    public async Task<PseoPage?> GetBySlugAsync(Guid projectId, string slug)
    {
        Guard.Against.Default(projectId);
        Guard.Against.NullOrWhiteSpace(slug);

        var results = await _db.QueryAsync<PseoPage>(
            "SELECT * FROM pseo_pages WHERE project_id = @ProjectId AND slug = @Slug LIMIT 1",
            new { ProjectId = projectId, Slug = slug });
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<PseoPage> CreateAsync(PseoPage page)
    {
        Guard.Against.Null(page);
        Guard.Against.Default(page.CollectionId);
        Guard.Against.Default(page.ProjectId);

        page.Id = Guid.NewGuid();
        page.CreatedAt = DateTime.UtcNow;
        page.UpdatedAt = DateTime.UtcNow;

        await _db.InsertAsync(page);
        return page;
    }

    /// <inheritdoc />
    public async Task<PseoPage> UpdateAsync(PseoPage page)
    {
        Guard.Against.Null(page);
        Guard.Against.Default(page.Id);

        page.UpdatedAt = DateTime.UtcNow;
        await _db.UpdateAsync(page);
        return page;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        Guard.Against.Default(id);

        var page = await _db.GetAsync<PseoPage>(id);
        if (page != null)
            await _db.DeleteAsync(page);
    }

    /// <inheritdoc />
    public async Task UpdateStatusAsync(Guid id, string status, string? validationErrors)
    {
        Guard.Against.Default(id);
        Guard.Against.NullOrWhiteSpace(status);

        await _db.ExecuteAsync(
            @"UPDATE pseo_pages
              SET status = @Status, validation_errors = @ValidationErrors, updated_at = @Now
              WHERE id = @Id",
            new { Status = status, ValidationErrors = validationErrors ?? "[]", Now = DateTime.UtcNow, Id = id });
    }

    /// <inheritdoc />
    public async Task<List<PseoPage>> BulkCreateAsync(List<PseoPage> pages)
    {
        Guard.Against.Null(pages);

        var created = new List<PseoPage>();
        foreach (var page in pages)
        {
            page.Id = Guid.NewGuid();
            page.CreatedAt = DateTime.UtcNow;
            page.UpdatedAt = DateTime.UtcNow;
            await _db.InsertAsync(page);
            created.Add(page);
        }

        _logger.LogInformation("Bulk created {Count} pSEO pages", created.Count);
        return created;
    }

    /// <inheritdoc />
    public async Task<List<PseoPage>> GetFailedPagesAsync(Guid collectionId)
    {
        Guard.Against.Default(collectionId);

        var results = await _db.QueryAsync<PseoPage>(
            "SELECT * FROM pseo_pages WHERE collection_id = @CollectionId AND status = @Status ORDER BY created_at",
            new { CollectionId = collectionId, Status = "failed" });
        return results.ToList();
    }

    /// <inheritdoc />
    public async Task<List<PseoPage>> GetPendingPublishAsync(Guid collectionId, int batchSize)
    {
        Guard.Against.Default(collectionId);

        var results = await _db.QueryAsync<PseoPage>(
            @"SELECT * FROM pseo_pages
              WHERE collection_id = @CollectionId AND status = @Status
              ORDER BY created_at
              LIMIT @Limit",
            new { CollectionId = collectionId, Status = "validated", Limit = batchSize });
        return results.ToList();
    }
}
