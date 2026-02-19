using System.Data;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for managing page layouts and their components, including
/// CRUD, component management, and default layout assignment.
/// </summary>
public class LayoutService : ILayoutService
{
    private readonly IDbConnection _db;
    private readonly ILogger<LayoutService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="LayoutService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="logger">The logger.</param>
    public LayoutService(IDbConnection db, ILogger<LayoutService> logger)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task<Layout?> GetByIdAsync(Guid id)
    {
        Guard.Against.Default(id);
        return await _db.GetAsync<Layout>(id);
    }

    /// <inheritdoc />
    public async Task<Layout?> GetBySlugAsync(Guid siteId, string slug)
    {
        Guard.Against.Default(siteId);
        Guard.Against.NullOrWhiteSpace(slug);

        var results = await _db.QueryAsync<Layout>(
            "SELECT * FROM layouts WHERE site_id = @SiteId AND slug = @Slug LIMIT 1",
            new { SiteId = siteId, Slug = slug });
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<(Layout Layout, IEnumerable<LayoutComponent> Components)?> GetWithComponentsAsync(Guid id)
    {
        Guard.Against.Default(id);

        var layout = await _db.GetAsync<Layout>(id);
        if (layout == null)
            return null;

        var components = await _db.QueryAsync<LayoutComponent>(
            "SELECT * FROM layout_components WHERE layout_id = @LayoutId ORDER BY region, sort_order",
            new { LayoutId = id });

        return (layout, components);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Layout>> GetAllBySiteAsync(Guid siteId, int page = 1, int pageSize = 50)
    {
        Guard.Against.Default(siteId);

        var offset = (Math.Max(page, 1) - 1) * pageSize;
        return await _db.QueryAsync<Layout>(
            "SELECT * FROM layouts WHERE site_id = @SiteId ORDER BY name LIMIT @Limit OFFSET @Offset",
            new { SiteId = siteId, Limit = pageSize, Offset = offset });
    }

    /// <inheritdoc />
    public async Task<Layout?> GetDefaultAsync(Guid siteId)
    {
        Guard.Against.Default(siteId);

        var results = await _db.QueryAsync<Layout>(
            "SELECT * FROM layouts WHERE site_id = @SiteId AND is_default = TRUE LIMIT 1",
            new { SiteId = siteId });
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<Layout> CreateAsync(Layout layout)
    {
        Guard.Against.Null(layout);
        Guard.Against.NullOrWhiteSpace(layout.Name);
        Guard.Against.Default(layout.SiteId);

        layout.Id = Guid.NewGuid();
        layout.CreatedAt = DateTime.UtcNow;
        layout.UpdatedAt = DateTime.UtcNow;

        await _db.InsertAsync(layout);
        return layout;
    }

    /// <inheritdoc />
    public async Task<Layout> UpdateAsync(Layout layout)
    {
        Guard.Against.Null(layout);
        Guard.Against.Default(layout.Id);

        layout.UpdatedAt = DateTime.UtcNow;
        await _db.UpdateAsync(layout);
        return layout;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        Guard.Against.Default(id);

        // Remove all components belonging to this layout
        await _db.ExecuteAsync(
            "DELETE FROM layout_components WHERE layout_id = @LayoutId",
            new { LayoutId = id });

        // Nullify layout reference on posts
        await _db.ExecuteAsync(
            "UPDATE posts SET layout_id = NULL WHERE layout_id = @Id",
            new { Id = id });

        var layout = await _db.GetAsync<Layout>(id);
        if (layout != null)
            await _db.DeleteAsync(layout);
    }

    /// <inheritdoc />
    public async Task SetDefaultAsync(Guid id)
    {
        Guard.Against.Default(id);

        var layout = await _db.GetAsync<Layout>(id);
        if (layout == null)
            return;

        // Clear the default flag on all layouts for this site
        await _db.ExecuteAsync(
            "UPDATE layouts SET is_default = FALSE WHERE site_id = @SiteId",
            new { SiteId = layout.SiteId });

        // Set the new default
        await _db.ExecuteAsync(
            "UPDATE layouts SET is_default = TRUE, updated_at = @Now WHERE id = @Id",
            new { Now = DateTime.UtcNow, Id = id });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LayoutComponent>> GetComponentsAsync(Guid layoutId)
    {
        Guard.Against.Default(layoutId);

        return await _db.QueryAsync<LayoutComponent>(
            "SELECT * FROM layout_components WHERE layout_id = @LayoutId ORDER BY region, sort_order",
            new { LayoutId = layoutId });
    }

    /// <inheritdoc />
    public async Task<LayoutComponent> AddComponentAsync(LayoutComponent component)
    {
        Guard.Against.Null(component);
        Guard.Against.Default(component.LayoutId);

        component.Id = Guid.NewGuid();
        component.CreatedAt = DateTime.UtcNow;
        component.UpdatedAt = DateTime.UtcNow;

        await _db.InsertAsync(component);
        return component;
    }

    /// <inheritdoc />
    public async Task<LayoutComponent> UpdateComponentAsync(LayoutComponent component)
    {
        Guard.Against.Null(component);
        Guard.Against.Default(component.Id);

        component.UpdatedAt = DateTime.UtcNow;
        await _db.UpdateAsync(component);
        return component;
    }

    /// <inheritdoc />
    public async Task RemoveComponentAsync(Guid componentId)
    {
        Guard.Against.Default(componentId);

        var component = await _db.GetAsync<LayoutComponent>(componentId);
        if (component != null)
            await _db.DeleteAsync(component);
    }
}
