using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing visual themes for public site rendering.
/// Handles CRUD operations, theme activation, and active theme retrieval.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Retrieves a theme by its unique identifier.
    /// </summary>
    /// <param name="id">The theme identifier.</param>
    /// <returns>The theme if found; otherwise null.</returns>
    Task<Theme?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves a theme by its slug.
    /// </summary>
    /// <param name="slug">The URL-friendly slug.</param>
    /// <returns>The theme if found; otherwise null.</returns>
    Task<Theme?> GetBySlugAsync(string slug);

    /// <summary>
    /// Retrieves all available themes with pagination.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A paginated collection of themes.</returns>
    Task<IEnumerable<Theme>> GetAllAsync(int page = 1, int pageSize = 50);

    /// <summary>
    /// Retrieves the currently active theme.
    /// </summary>
    /// <returns>The active theme if one is set; otherwise null.</returns>
    Task<Theme?> GetActiveAsync();

    /// <summary>
    /// Creates a new theme.
    /// </summary>
    /// <param name="theme">The theme to create.</param>
    /// <returns>The created theme with generated identifier.</returns>
    Task<Theme> CreateAsync(Theme theme);

    /// <summary>
    /// Updates an existing theme.
    /// </summary>
    /// <param name="theme">The theme with updated fields.</param>
    /// <returns>The updated theme.</returns>
    Task<Theme> UpdateAsync(Theme theme);

    /// <summary>
    /// Deletes a theme. Cannot delete the currently active theme.
    /// Sites using this theme will have their theme_id set to null.
    /// </summary>
    /// <param name="id">The theme identifier.</param>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Activates a theme, deactivating the previously active theme.
    /// Only one theme can be active at a time.
    /// </summary>
    /// <param name="id">The theme identifier to activate.</param>
    Task ActivateAsync(Guid id);

    /// <summary>
    /// Updates the CSS variables configuration for a theme.
    /// </summary>
    /// <param name="id">The theme identifier.</param>
    /// <param name="cssVariablesJson">The CSS variables as a JSON string.</param>
    Task UpdateCssVariablesAsync(Guid id, string cssVariablesJson);

    /// <summary>
    /// Updates the settings for a theme.
    /// </summary>
    /// <param name="id">The theme identifier.</param>
    /// <param name="settingsJson">The settings as a JSON string.</param>
    Task UpdateSettingsAsync(Guid id, string settingsJson);
}
