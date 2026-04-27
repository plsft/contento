using System.Data;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for managing niche taxonomies — system-provided and project-custom niches for pSEO content.
/// </summary>
public class NicheService : INicheService
{
    private readonly IDbConnection _db;
    private readonly ILogger<NicheService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="NicheService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="logger">The logger.</param>
    public NicheService(IDbConnection db, ILogger<NicheService> logger)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task<NicheTaxonomy?> GetByIdAsync(Guid id)
    {
        Guard.Against.Default(id);
        return await _db.GetAsync<NicheTaxonomy>(id);
    }

    /// <inheritdoc />
    public async Task<NicheTaxonomy?> GetBySlugAsync(string slug)
    {
        Guard.Against.NullOrWhiteSpace(slug);

        var results = await _db.QueryAsync<NicheTaxonomy>(
            "SELECT * FROM niche_taxonomies WHERE slug = @Slug LIMIT 1",
            new { Slug = slug });
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<List<NicheTaxonomy>> GetAllSystemAsync()
    {
        var results = await _db.QueryAsync<NicheTaxonomy>(
            "SELECT * FROM niche_taxonomies WHERE is_system = true ORDER BY category, name");
        return results.ToList();
    }

    /// <inheritdoc />
    public async Task<List<NicheTaxonomy>> GetByProjectIdAsync(Guid projectId)
    {
        Guard.Against.Default(projectId);

        var results = await _db.QueryAsync<NicheTaxonomy>(
            "SELECT * FROM niche_taxonomies WHERE project_id = @ProjectId ORDER BY name",
            new { ProjectId = projectId });
        return results.ToList();
    }

    /// <inheritdoc />
    public async Task<List<NicheTaxonomy>> SearchAsync(string? query, string? category)
    {
        var conditions = new List<string>();
        var parameters = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(query))
        {
            conditions.Add("(name ILIKE @Query OR slug ILIKE @Query)");
            parameters["Query"] = $"%{query}%";
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            conditions.Add("(category = @Category OR @Category IS NULL)");
            parameters["Category"] = category;
        }

        var whereClause = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : "";

        var sql = $"SELECT * FROM niche_taxonomies {whereClause} ORDER BY category, name";
        var results = await _db.QueryAsync<NicheTaxonomy>(sql, parameters);
        return results.ToList();
    }

    /// <inheritdoc />
    public async Task<NicheTaxonomy> CreateAsync(NicheTaxonomy niche)
    {
        Guard.Against.Null(niche);
        Guard.Against.NullOrWhiteSpace(niche.Name);
        Guard.Against.NullOrWhiteSpace(niche.Slug);

        niche.Id = Guid.NewGuid();
        niche.CreatedAt = DateTime.UtcNow;
        niche.UpdatedAt = DateTime.UtcNow;

        await _db.InsertAsync(niche);
        return niche;
    }

    /// <inheritdoc />
    public async Task<NicheTaxonomy> UpdateAsync(NicheTaxonomy niche)
    {
        Guard.Against.Null(niche);
        Guard.Against.Default(niche.Id);

        niche.UpdatedAt = DateTime.UtcNow;
        await _db.UpdateAsync(niche);
        return niche;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        Guard.Against.Default(id);

        var niche = await _db.GetAsync<NicheTaxonomy>(id);
        if (niche != null)
            await _db.DeleteAsync(niche);
    }

    /// <inheritdoc />
    public async Task<NicheTaxonomy> ForkAsync(Guid nicheId, Guid projectId)
    {
        Guard.Against.Default(nicheId);
        Guard.Against.Default(projectId);

        var source = await _db.GetAsync<NicheTaxonomy>(nicheId)
            ?? throw new InvalidOperationException($"Niche {nicheId} not found");

        var fork = new NicheTaxonomy
        {
            Id = Guid.NewGuid(),
            Slug = $"{source.Slug}-{projectId.ToString()[..8]}",
            Name = source.Name,
            Category = source.Category,
            Context = source.Context,
            IsSystem = false,
            ProjectId = projectId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _db.InsertAsync(fork);
        _logger.LogInformation("Forked system niche {SourceId} to project niche {ForkId} for project {ProjectId}",
            nicheId, fork.Id, projectId);

        return fork;
    }
}
