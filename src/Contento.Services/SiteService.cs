using System.Data;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for managing Contento site instances, including lookup by slug/domain,
/// settings updates, and theme assignment.
/// </summary>
public class SiteService : ISiteService
{
    private readonly IDbConnection _db;
    private readonly ILogger<SiteService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SiteService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="logger">The logger.</param>
    public SiteService(IDbConnection db, ILogger<SiteService> logger)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task<Site?> GetByIdAsync(Guid id)
    {
        Guard.Against.Default(id);
        return await _db.GetAsync<Site>(id);
    }

    /// <inheritdoc />
    public async Task<Site?> GetBySlugAsync(string slug)
    {
        Guard.Against.NullOrWhiteSpace(slug);

        var results = await _db.QueryAsync<Site>(
            "SELECT * FROM sites WHERE slug = @Slug LIMIT 1",
            new { Slug = slug });
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<Site?> GetByDomainAsync(string domain)
    {
        Guard.Against.NullOrWhiteSpace(domain);

        var results = await _db.QueryAsync<Site>(
            "SELECT * FROM sites WHERE domain = @Domain LIMIT 1",
            new { Domain = domain });
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Site>> GetAllAsync(int page = 1, int pageSize = 50)
    {
        var offset = (Math.Max(page, 1) - 1) * pageSize;
        return await _db.QueryAsync<Site>(
            "SELECT * FROM sites ORDER BY name LIMIT @Limit OFFSET @Offset",
            new { Limit = pageSize, Offset = offset });
    }

    /// <inheritdoc />
    public async Task<Site> CreateAsync(Site site)
    {
        Guard.Against.Null(site);
        Guard.Against.NullOrWhiteSpace(site.Name);
        Guard.Against.NullOrWhiteSpace(site.Slug);

        site.Id = Guid.NewGuid();
        site.CreatedAt = DateTime.UtcNow;
        site.UpdatedAt = DateTime.UtcNow;

        await _db.InsertAsync(site);
        return site;
    }

    /// <inheritdoc />
    public async Task<Site> UpdateAsync(Site site)
    {
        Guard.Against.Null(site);
        Guard.Against.Default(site.Id);

        site.UpdatedAt = DateTime.UtcNow;
        await _db.UpdateAsync(site);
        return site;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        Guard.Against.Default(id);

        var site = await _db.GetAsync<Site>(id);
        if (site != null)
            await _db.DeleteAsync(site);
    }

    /// <inheritdoc />
    public async Task UpdateSettingsAsync(Guid id, string settingsJson)
    {
        Guard.Against.Default(id);
        Guard.Against.NullOrWhiteSpace(settingsJson);

        await _db.ExecuteAsync(
            "UPDATE sites SET settings = @Settings::jsonb, updated_at = @Now WHERE id = @Id",
            new { Settings = settingsJson, Now = DateTime.UtcNow, Id = id });
    }

    /// <inheritdoc />
    public async Task SetThemeAsync(Guid id, Guid themeId)
    {
        Guard.Against.Default(id);
        Guard.Against.Default(themeId);

        await _db.ExecuteAsync(
            "UPDATE sites SET theme_id = @ThemeId, updated_at = @Now WHERE id = @Id",
            new { ThemeId = themeId, Now = DateTime.UtcNow, Id = id });
    }
}
