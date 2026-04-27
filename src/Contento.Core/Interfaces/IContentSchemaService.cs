using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing content schemas that define the structure of pSEO page content.
/// </summary>
public interface IContentSchemaService
{
    /// <summary>
    /// Retrieves a content schema by its unique identifier.
    /// </summary>
    Task<ContentSchema?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves a content schema by its URL-friendly slug.
    /// </summary>
    Task<ContentSchema?> GetBySlugAsync(string slug);

    /// <summary>
    /// Returns all system-level content schemas.
    /// </summary>
    Task<List<ContentSchema>> GetAllSystemAsync();

    /// <summary>
    /// Returns all content schemas (system + custom).
    /// </summary>
    Task<List<ContentSchema>> GetAllAsync();

    /// <summary>
    /// Creates a new content schema.
    /// </summary>
    Task<ContentSchema> CreateAsync(ContentSchema schema);

    /// <summary>
    /// Updates an existing content schema.
    /// </summary>
    Task<ContentSchema> UpdateAsync(ContentSchema schema);

    /// <summary>
    /// Deletes a content schema.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Validates a JSON content string against the specified schema.
    /// </summary>
    /// <param name="schemaId">The schema to validate against.</param>
    /// <param name="contentJson">The JSON content to validate.</param>
    /// <returns>A tuple indicating validity and any validation errors.</returns>
    Task<(bool IsValid, List<string> Errors)> ValidateContentAsync(Guid schemaId, string contentJson);
}
