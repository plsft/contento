using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing installed plugins on a site.
/// Handles install/uninstall, enable/disable, settings management, and listing.
/// </summary>
public interface IPluginService
{
    /// <summary>
    /// Retrieves an installed plugin by its unique identifier.
    /// </summary>
    /// <param name="id">The plugin identifier.</param>
    /// <returns>The installed plugin if found; otherwise null.</returns>
    Task<InstalledPlugin?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves an installed plugin by its slug within a specific site.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="slug">The plugin slug.</param>
    /// <returns>The installed plugin if found; otherwise null.</returns>
    Task<InstalledPlugin?> GetBySlugAsync(Guid siteId, string slug);

    /// <summary>
    /// Retrieves all installed plugins for a site with pagination.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="enabledOnly">If true, only returns enabled plugins.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A paginated collection of installed plugins.</returns>
    Task<IEnumerable<InstalledPlugin>> GetAllBySiteAsync(Guid siteId, bool? enabledOnly = null,
        int page = 1, int pageSize = 50);

    /// <summary>
    /// Installs a plugin on a site. Validates the plugin manifest,
    /// checks permissions, and creates the installed plugin record.
    /// </summary>
    /// <param name="plugin">The plugin to install.</param>
    /// <returns>The installed plugin record.</returns>
    Task<InstalledPlugin> InstallAsync(InstalledPlugin plugin);

    /// <summary>
    /// Uninstalls a plugin from a site. Disables the plugin, cleans up
    /// any plugin-scoped storage, and removes the installed plugin record.
    /// </summary>
    /// <param name="id">The plugin identifier.</param>
    Task UninstallAsync(Guid id);

    /// <summary>
    /// Enables a previously disabled plugin on a site.
    /// </summary>
    /// <param name="id">The plugin identifier.</param>
    Task EnableAsync(Guid id);

    /// <summary>
    /// Disables an enabled plugin on a site. The plugin remains installed
    /// but will not execute any hooks or render UI components.
    /// </summary>
    /// <param name="id">The plugin identifier.</param>
    Task DisableAsync(Guid id);

    /// <summary>
    /// Retrieves the settings for an installed plugin as a JSON string.
    /// </summary>
    /// <param name="id">The plugin identifier.</param>
    /// <returns>The plugin settings as a JSON string.</returns>
    Task<string> GetSettingsAsync(Guid id);

    /// <summary>
    /// Updates the settings for an installed plugin.
    /// </summary>
    /// <param name="id">The plugin identifier.</param>
    /// <param name="settingsJson">The settings as a JSON string.</param>
    Task UpdateSettingsAsync(Guid id, string settingsJson);

    /// <summary>
    /// Returns the total count of installed plugins for a site.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="enabledOnly">If true, only counts enabled plugins.</param>
    /// <returns>The total count of matching installed plugins.</returns>
    Task<int> GetTotalCountAsync(Guid siteId, bool? enabledOnly = null);
}
