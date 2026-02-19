using System.Data;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for managing installed plugins: install/uninstall, enable/disable,
/// settings management, and site-scoped listing.
/// </summary>
public class PluginService : IPluginService
{
    private readonly IDbConnection _db;
    private readonly ILogger<PluginService> _logger;

    public PluginService(IDbConnection db, ILogger<PluginService> logger)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task<InstalledPlugin?> GetByIdAsync(Guid id)
    {
        Guard.Against.Default(id);
        return await _db.GetAsync<InstalledPlugin>(id);
    }

    /// <inheritdoc />
    public async Task<InstalledPlugin?> GetBySlugAsync(Guid siteId, string slug)
    {
        Guard.Against.Default(siteId);
        Guard.Against.NullOrWhiteSpace(slug);

        var results = await _db.QueryAsync<InstalledPlugin>(
            "SELECT * FROM installed_plugins WHERE site_id = @SiteId AND slug = @Slug LIMIT 1",
            new { SiteId = siteId, Slug = slug });
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<InstalledPlugin>> GetAllBySiteAsync(Guid siteId, bool? enabledOnly = null,
        int page = 1, int pageSize = 50)
    {
        Guard.Against.Default(siteId);

        var offset = (Math.Max(page, 1) - 1) * pageSize;

        if (enabledOnly == true)
        {
            return await _db.QueryAsync<InstalledPlugin>(
                "SELECT * FROM installed_plugins WHERE site_id = @SiteId AND is_enabled = TRUE ORDER BY name LIMIT @Limit OFFSET @Offset",
                new { SiteId = siteId, Limit = pageSize, Offset = offset });
        }

        return await _db.QueryAsync<InstalledPlugin>(
            "SELECT * FROM installed_plugins WHERE site_id = @SiteId ORDER BY name LIMIT @Limit OFFSET @Offset",
            new { SiteId = siteId, Limit = pageSize, Offset = offset });
    }

    /// <inheritdoc />
    public async Task<InstalledPlugin> InstallAsync(InstalledPlugin plugin)
    {
        Guard.Against.Null(plugin);
        Guard.Against.Default(plugin.SiteId);
        Guard.Against.NullOrWhiteSpace(plugin.Name);
        Guard.Against.NullOrWhiteSpace(plugin.Slug);

        plugin.Id = Guid.NewGuid();
        plugin.IsEnabled = true;
        plugin.InstalledAt = DateTime.UtcNow;
        plugin.UpdatedAt = DateTime.UtcNow;

        await _db.InsertAsync(plugin);
        return plugin;
    }

    /// <inheritdoc />
    public async Task UninstallAsync(Guid id)
    {
        Guard.Against.Default(id);

        var plugin = await _db.GetAsync<InstalledPlugin>(id);
        if (plugin != null)
            await _db.DeleteAsync(plugin);
    }

    /// <inheritdoc />
    public async Task EnableAsync(Guid id)
    {
        Guard.Against.Default(id);

        await _db.ExecuteAsync(
            "UPDATE installed_plugins SET is_enabled = TRUE, updated_at = @Now WHERE id = @Id",
            new { Now = DateTime.UtcNow, Id = id });
    }

    /// <inheritdoc />
    public async Task DisableAsync(Guid id)
    {
        Guard.Against.Default(id);

        await _db.ExecuteAsync(
            "UPDATE installed_plugins SET is_enabled = FALSE, updated_at = @Now WHERE id = @Id",
            new { Now = DateTime.UtcNow, Id = id });
    }

    /// <inheritdoc />
    public async Task<string> GetSettingsAsync(Guid id)
    {
        Guard.Against.Default(id);

        var plugin = await _db.GetAsync<InstalledPlugin>(id);
        return plugin?.Settings ?? "{}";
    }

    /// <inheritdoc />
    public async Task UpdateSettingsAsync(Guid id, string settingsJson)
    {
        Guard.Against.Default(id);
        Guard.Against.NullOrWhiteSpace(settingsJson);

        await _db.ExecuteAsync(
            "UPDATE installed_plugins SET settings = @Settings::jsonb, updated_at = @Now WHERE id = @Id",
            new { Settings = settingsJson, Now = DateTime.UtcNow, Id = id });
    }

    /// <inheritdoc />
    public async Task<int> GetTotalCountAsync(Guid siteId, bool? enabledOnly = null)
    {
        Guard.Against.Default(siteId);

        if (enabledOnly == true)
        {
            return await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM installed_plugins WHERE site_id = @SiteId AND is_enabled = TRUE",
                new { SiteId = siteId });
        }

        return await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM installed_plugins WHERE site_id = @SiteId",
            new { SiteId = siteId });
    }
}
