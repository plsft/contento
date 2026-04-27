namespace Contento.Core.Interfaces;

/// <summary>
/// Low-level schema validation service for validating JSON content against content schema definitions.
/// </summary>
public interface ISchemaValidationService
{
    /// <summary>
    /// Validates a JSON content string against a schema definition.
    /// </summary>
    /// <param name="schemaJson">The JSON schema definition.</param>
    /// <param name="contentJson">The JSON content to validate.</param>
    /// <returns>A tuple indicating validity and any validation error messages.</returns>
    Task<(bool IsValid, List<string> Errors)> ValidateAsync(string schemaJson, string contentJson);

    /// <summary>
    /// Validates field-level constraints such as exact counts, string lengths, and enum values.
    /// </summary>
    /// <param name="schemaJson">The JSON schema definition.</param>
    /// <param name="contentJson">The JSON content to validate.</param>
    /// <returns>A tuple indicating validity and any constraint violation messages.</returns>
    Task<(bool IsValid, List<string> Errors)> ValidateFieldConstraintsAsync(string schemaJson, string contentJson);
}
