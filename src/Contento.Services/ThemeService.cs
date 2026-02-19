using System.Data;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for managing visual themes including CRUD, activation,
/// and CSS variable / settings management.
/// </summary>
public class ThemeService : IThemeService
{
    private readonly IDbConnection _db;
    private readonly ILogger<ThemeService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ThemeService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="logger">The logger.</param>
    public ThemeService(IDbConnection db, ILogger<ThemeService> logger)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task<Theme?> GetByIdAsync(Guid id)
    {
        Guard.Against.Default(id);
        return await _db.GetAsync<Theme>(id);
    }

    /// <inheritdoc />
    public async Task<Theme?> GetBySlugAsync(string slug)
    {
        Guard.Against.NullOrWhiteSpace(slug);

        var results = await _db.QueryAsync<Theme>(
            "SELECT * FROM themes WHERE slug = @Slug LIMIT 1",
            new { Slug = slug });
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Theme>> GetAllAsync(int page = 1, int pageSize = 50)
    {
        var offset = (Math.Max(page, 1) - 1) * pageSize;
        return await _db.QueryAsync<Theme>(
            "SELECT * FROM themes ORDER BY name LIMIT @Limit OFFSET @Offset",
            new { Limit = pageSize, Offset = offset });
    }

    /// <inheritdoc />
    public async Task<Theme?> GetActiveAsync()
    {
        var results = await _db.QueryAsync<Theme>(
            "SELECT * FROM themes WHERE is_active = TRUE LIMIT 1",
            new { });
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<Theme> CreateAsync(Theme theme)
    {
        Guard.Against.Null(theme);
        Guard.Against.NullOrWhiteSpace(theme.Name);

        theme.Id = Guid.NewGuid();
        theme.CreatedAt = DateTime.UtcNow;

        await _db.InsertAsync(theme);
        return theme;
    }

    /// <inheritdoc />
    public async Task<Theme> UpdateAsync(Theme theme)
    {
        Guard.Against.Null(theme);
        Guard.Against.Default(theme.Id);

        await _db.UpdateAsync(theme);
        return theme;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        Guard.Against.Default(id);

        // Nullify theme reference on sites
        await _db.ExecuteAsync(
            "UPDATE sites SET theme_id = NULL WHERE theme_id = @Id",
            new { Id = id });

        var theme = await _db.GetAsync<Theme>(id);
        if (theme != null)
            await _db.DeleteAsync(theme);
    }

    /// <inheritdoc />
    public async Task ActivateAsync(Guid id)
    {
        Guard.Against.Default(id);

        // Deactivate all themes
        await _db.ExecuteAsync(
            "UPDATE themes SET is_active = FALSE WHERE is_active = TRUE",
            new { });

        // Activate the specified theme
        await _db.ExecuteAsync(
            "UPDATE themes SET is_active = TRUE WHERE id = @Id",
            new { Id = id });
    }

    /// <inheritdoc />
    public async Task UpdateCssVariablesAsync(Guid id, string cssVariablesJson)
    {
        Guard.Against.Default(id);
        Guard.Against.NullOrWhiteSpace(cssVariablesJson);

        await _db.ExecuteAsync(
            "UPDATE themes SET css_variables = @CssVariables::jsonb WHERE id = @Id",
            new { CssVariables = cssVariablesJson, Id = id });
    }

    /// <inheritdoc />
    public async Task UpdateSettingsAsync(Guid id, string settingsJson)
    {
        Guard.Against.Default(id);
        Guard.Against.NullOrWhiteSpace(settingsJson);

        await _db.ExecuteAsync(
            "UPDATE themes SET settings = @Settings::jsonb WHERE id = @Id",
            new { Settings = settingsJson, Id = id });
    }
}
