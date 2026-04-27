using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing niche taxonomies — system-provided and project-custom niches for pSEO content.
/// </summary>
public interface INicheService
{
    /// <summary>
    /// Retrieves a niche by its unique identifier.
    /// </summary>
    Task<NicheTaxonomy?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves a niche by its URL-friendly slug.
    /// </summary>
    Task<NicheTaxonomy?> GetBySlugAsync(string slug);

    /// <summary>
    /// Returns all system-level niches (not project-specific).
    /// </summary>
    Task<List<NicheTaxonomy>> GetAllSystemAsync();

    /// <summary>
    /// Returns all custom niches belonging to a specific project.
    /// </summary>
    Task<List<NicheTaxonomy>> GetByProjectIdAsync(Guid projectId);

    /// <summary>
    /// Searches and filters niches by query string and/or category.
    /// </summary>
    Task<List<NicheTaxonomy>> SearchAsync(string? query, string? category);

    /// <summary>
    /// Creates a new niche taxonomy entry.
    /// </summary>
    Task<NicheTaxonomy> CreateAsync(NicheTaxonomy niche);

    /// <summary>
    /// Updates an existing niche taxonomy entry.
    /// </summary>
    Task<NicheTaxonomy> UpdateAsync(NicheTaxonomy niche);

    /// <summary>
    /// Deletes a niche taxonomy entry.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Forks a system niche into a project-specific customizable copy.
    /// </summary>
    Task<NicheTaxonomy> ForkAsync(Guid nicheId, Guid projectId);
}
