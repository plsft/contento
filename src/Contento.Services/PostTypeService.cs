using System.Data;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for managing custom post type definitions, including CRUD operations
/// with system post type protection.
/// </summary>
public class PostTypeService : IPostTypeService
{
    private readonly IDbConnection _db;
    private readonly ILogger<PostTypeService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PostTypeService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="logger">The logger.</param>
    public PostTypeService(IDbConnection db, ILogger<PostTypeService> logger)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task<PostType?> GetByIdAsync(Guid id)
    {
        Guard.Against.Default(id);
        return await _db.GetAsync<PostType>(id);
    }

    /// <inheritdoc />
    public async Task<PostType?> GetBySlugAsync(Guid siteId, string slug)
    {
        Guard.Against.Default(siteId);
        Guard.Against.NullOrWhiteSpace(slug);

        var results = await _db.QueryAsync<PostType>(
            "SELECT * FROM post_types WHERE site_id = @SiteId AND slug = @Slug LIMIT 1",
            new { SiteId = siteId, Slug = slug });
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PostType>> GetAllAsync(Guid siteId)
    {
        Guard.Against.Default(siteId);

        return await _db.QueryAsync<PostType>(
            "SELECT * FROM post_types WHERE site_id = @SiteId ORDER BY sort_order, name",
            new { SiteId = siteId });
    }

    /// <inheritdoc />
    public async Task<PostType> CreateAsync(PostType postType)
    {
        Guard.Against.Null(postType);
        Guard.Against.NullOrWhiteSpace(postType.Name);
        Guard.Against.NullOrWhiteSpace(postType.Slug);

        postType.Id = Guid.NewGuid();
        postType.CreatedAt = DateTime.UtcNow;

        await _db.InsertAsync(postType);
        return postType;
    }

    /// <inheritdoc />
    public async Task<PostType> UpdateAsync(PostType postType)
    {
        Guard.Against.Null(postType);
        Guard.Against.Default(postType.Id);

        await _db.UpdateAsync(postType);
        return postType;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        Guard.Against.Default(id);

        var postType = await _db.GetAsync<PostType>(id);
        if (postType == null)
            return;

        if (postType.IsSystem)
            throw new InvalidOperationException("Cannot delete system post type");

        await _db.DeleteAsync(postType);
    }
}
