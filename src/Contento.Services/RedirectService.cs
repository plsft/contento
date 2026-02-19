using System.Data;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for managing URL redirects (301/302) with CRUD, path-based lookup,
/// hit tracking, and automatic slug-change redirect creation.
/// </summary>
public class RedirectService : IRedirectService
{
    private readonly IDbConnection _db;
    private readonly ILogger<RedirectService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="RedirectService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="logger">The logger.</param>
    public RedirectService(IDbConnection db, ILogger<RedirectService> logger)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task<Redirect?> GetByIdAsync(Guid id)
    {
        Guard.Against.Default(id);
        return await _db.GetAsync<Redirect>(id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Redirect>> GetAllAsync(Guid siteId, int page = 1, int pageSize = 50)
    {
        Guard.Against.Default(siteId);

        var offset = (Math.Max(page, 1) - 1) * pageSize;
        return await _db.QueryAsync<Redirect>(
            "SELECT * FROM redirects WHERE site_id = @SiteId ORDER BY created_at DESC LIMIT @Limit OFFSET @Offset",
            new { SiteId = siteId, Limit = pageSize, Offset = offset });
    }

    /// <inheritdoc />
    public async Task<Redirect?> GetByFromPathAsync(Guid siteId, string fromPath)
    {
        Guard.Against.Default(siteId);
        Guard.Against.NullOrWhiteSpace(fromPath);

        var results = await _db.QueryAsync<Redirect>(
            "SELECT * FROM redirects WHERE site_id = @SiteId AND from_path = @FromPath AND is_active = true LIMIT 1",
            new { SiteId = siteId, FromPath = fromPath });
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<Redirect> CreateAsync(Redirect redirect)
    {
        Guard.Against.Null(redirect);
        Guard.Against.NullOrWhiteSpace(redirect.FromPath);
        Guard.Against.NullOrWhiteSpace(redirect.ToPath);
        Guard.Against.Default(redirect.SiteId);

        redirect.Id = Guid.NewGuid();
        redirect.CreatedAt = DateTime.UtcNow;

        await _db.InsertAsync(redirect);
        return redirect;
    }

    /// <inheritdoc />
    public async Task<Redirect> UpdateAsync(Redirect redirect)
    {
        Guard.Against.Null(redirect);
        Guard.Against.Default(redirect.Id);

        await _db.UpdateAsync(redirect);
        return redirect;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        Guard.Against.Default(id);

        var redirect = await _db.GetAsync<Redirect>(id);
        if (redirect == null)
            return;

        await _db.DeleteAsync(redirect);
    }

    /// <inheritdoc />
    public async Task IncrementHitCountAsync(Guid id)
    {
        Guard.Against.Default(id);

        await _db.ExecuteAsync(
            "UPDATE redirects SET hit_count = hit_count + 1, last_hit_at = CURRENT_TIMESTAMP WHERE id = @Id",
            new { Id = id });
    }

    /// <inheritdoc />
    public async Task<int> GetTotalCountAsync(Guid siteId)
    {
        Guard.Against.Default(siteId);

        return await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM redirects WHERE site_id = @SiteId",
            new { SiteId = siteId });
    }

    /// <inheritdoc />
    public async Task CreateSlugChangeRedirectAsync(Guid siteId, string oldSlug, string newSlug)
    {
        Guard.Against.Default(siteId);
        Guard.Against.NullOrWhiteSpace(oldSlug);
        Guard.Against.NullOrWhiteSpace(newSlug);

        var fromPath = $"/{oldSlug}";
        var toPath = $"/{newSlug}";

        // Check if a redirect from this path already exists
        var existing = await GetByFromPathAsync(siteId, fromPath);
        if (existing != null)
        {
            _logger.LogDebug("Redirect from {FromPath} already exists, skipping creation", fromPath);
            return;
        }

        var redirect = new Redirect
        {
            SiteId = siteId,
            FromPath = fromPath,
            ToPath = toPath,
            StatusCode = 301,
            IsActive = true,
            Notes = "Auto-created on slug change"
        };

        await CreateAsync(redirect);
        _logger.LogInformation("Created slug-change redirect: {FromPath} -> {ToPath}", fromPath, toPath);
    }
}
