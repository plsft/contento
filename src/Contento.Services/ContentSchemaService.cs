using System.Data;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for managing content schemas that define the structure of pSEO page content.
/// </summary>
public class ContentSchemaService : IContentSchemaService
{
    private readonly IDbConnection _db;
    private readonly ISchemaValidationService _schemaValidation;
    private readonly ILogger<ContentSchemaService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ContentSchemaService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="schemaValidation">The schema validation service.</param>
    /// <param name="logger">The logger.</param>
    public ContentSchemaService(IDbConnection db, ISchemaValidationService schemaValidation, ILogger<ContentSchemaService> logger)
    {
        _db = Guard.Against.Null(db);
        _schemaValidation = Guard.Against.Null(schemaValidation);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task<ContentSchema?> GetByIdAsync(Guid id)
    {
        Guard.Against.Default(id);
        return await _db.GetAsync<ContentSchema>(id);
    }

    /// <inheritdoc />
    public async Task<ContentSchema?> GetBySlugAsync(string slug)
    {
        Guard.Against.NullOrWhiteSpace(slug);

        var results = await _db.QueryAsync<ContentSchema>(
            "SELECT * FROM content_schemas WHERE slug = @Slug LIMIT 1",
            new { Slug = slug });
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<List<ContentSchema>> GetAllSystemAsync()
    {
        var results = await _db.QueryAsync<ContentSchema>(
            "SELECT * FROM content_schemas WHERE is_system = true ORDER BY name");
        return results.ToList();
    }

    /// <inheritdoc />
    public async Task<List<ContentSchema>> GetAllAsync()
    {
        var results = await _db.QueryAsync<ContentSchema>(
            "SELECT * FROM content_schemas ORDER BY name");
        return results.ToList();
    }

    /// <inheritdoc />
    public async Task<ContentSchema> CreateAsync(ContentSchema schema)
    {
        Guard.Against.Null(schema);
        Guard.Against.NullOrWhiteSpace(schema.Name);
        Guard.Against.NullOrWhiteSpace(schema.Slug);

        schema.Id = Guid.NewGuid();
        schema.CreatedAt = DateTime.UtcNow;
        schema.UpdatedAt = DateTime.UtcNow;

        await _db.InsertAsync(schema);
        return schema;
    }

    /// <inheritdoc />
    public async Task<ContentSchema> UpdateAsync(ContentSchema schema)
    {
        Guard.Against.Null(schema);
        Guard.Against.Default(schema.Id);

        schema.UpdatedAt = DateTime.UtcNow;
        await _db.UpdateAsync(schema);
        return schema;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        Guard.Against.Default(id);

        var schema = await _db.GetAsync<ContentSchema>(id);
        if (schema != null)
            await _db.DeleteAsync(schema);
    }

    /// <inheritdoc />
    public async Task<(bool IsValid, List<string> Errors)> ValidateContentAsync(Guid schemaId, string contentJson)
    {
        Guard.Against.Default(schemaId);
        Guard.Against.NullOrWhiteSpace(contentJson);

        var schema = await _db.GetAsync<ContentSchema>(schemaId)
            ?? throw new InvalidOperationException($"Content schema {schemaId} not found");

        return await _schemaValidation.ValidateAsync(schema.SchemaJson, contentJson);
    }
}
