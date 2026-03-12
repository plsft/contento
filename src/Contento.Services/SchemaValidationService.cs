using System.Text.Json;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;

namespace Contento.Services;

/// <summary>
/// Low-level schema validation service for validating AI-generated JSON content against content schema definitions.
/// </summary>
public class SchemaValidationService : ISchemaValidationService
{
    private readonly ILogger<SchemaValidationService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SchemaValidationService"/>.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public SchemaValidationService(ILogger<SchemaValidationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<(bool IsValid, List<string> Errors)> ValidateAsync(string schemaJson, string contentJson)
    {
        var errors = new List<string>();

        JsonDocument? schemaDoc = null;
        JsonDocument? contentDoc = null;

        try
        {
            schemaDoc = JsonDocument.Parse(schemaJson);
        }
        catch (JsonException ex)
        {
            errors.Add($"Schema JSON is invalid: {ex.Message}");
            return Task.FromResult((false, errors));
        }

        try
        {
            contentDoc = JsonDocument.Parse(contentJson);
        }
        catch (JsonException ex)
        {
            errors.Add($"Content JSON is invalid: {ex.Message}");
            schemaDoc.Dispose();
            return Task.FromResult((false, errors));
        }

        using (schemaDoc)
        using (contentDoc)
        {
            var schemaRoot = schemaDoc.RootElement;
            var contentRoot = contentDoc.RootElement;

            // Walk through schema properties and validate content
            if (schemaRoot.TryGetProperty("properties", out var properties))
            {
                // Check required fields
                var requiredFields = new List<string>();
                if (schemaRoot.TryGetProperty("required", out var requiredArray) &&
                    requiredArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in requiredArray.EnumerateArray())
                    {
                        var fieldName = item.GetString();
                        if (!string.IsNullOrEmpty(fieldName))
                            requiredFields.Add(fieldName);
                    }
                }

                foreach (var fieldName in requiredFields)
                {
                    if (!contentRoot.TryGetProperty(fieldName, out var fieldValue))
                    {
                        errors.Add($"Required field '{fieldName}' is missing.");
                        continue;
                    }

                    // Check that the field has a meaningful value
                    if (fieldValue.ValueKind == JsonValueKind.Null)
                    {
                        errors.Add($"Required field '{fieldName}' is null.");
                    }
                }

                // Validate each property that exists in the schema
                foreach (var schemaProp in properties.EnumerateObject())
                {
                    var fieldName = schemaProp.Name;
                    if (!contentRoot.TryGetProperty(fieldName, out var contentValue))
                        continue; // Skip non-required missing fields (already handled above)

                    ValidateFieldType(fieldName, schemaProp.Value, contentValue, errors);
                }
            }
            else
            {
                // Schema has no "properties" key — treat all top-level content keys as valid
                // but verify content is at least a valid JSON object
                if (contentRoot.ValueKind != JsonValueKind.Object)
                {
                    errors.Add("Content must be a JSON object.");
                }
            }
        }

        var isValid = errors.Count == 0;
        return Task.FromResult((isValid, errors));
    }

    /// <inheritdoc />
    public Task<(bool IsValid, List<string> Errors)> ValidateFieldConstraintsAsync(string schemaJson, string contentJson)
    {
        var errors = new List<string>();

        JsonDocument? schemaDoc = null;
        JsonDocument? contentDoc = null;

        try
        {
            schemaDoc = JsonDocument.Parse(schemaJson);
        }
        catch (JsonException ex)
        {
            errors.Add($"Schema JSON is invalid: {ex.Message}");
            return Task.FromResult((false, errors));
        }

        try
        {
            contentDoc = JsonDocument.Parse(contentJson);
        }
        catch (JsonException ex)
        {
            errors.Add($"Content JSON is invalid: {ex.Message}");
            schemaDoc.Dispose();
            return Task.FromResult((false, errors));
        }

        using (schemaDoc)
        using (contentDoc)
        {
            var schemaRoot = schemaDoc.RootElement;
            var contentRoot = contentDoc.RootElement;

            if (!schemaRoot.TryGetProperty("properties", out var properties))
            {
                return Task.FromResult((true, errors));
            }

            foreach (var schemaProp in properties.EnumerateObject())
            {
                var fieldName = schemaProp.Name;
                if (!contentRoot.TryGetProperty(fieldName, out var contentValue))
                    continue;

                var fieldSchema = schemaProp.Value;

                // Check array constraints (minItems, maxItems)
                if (contentValue.ValueKind == JsonValueKind.Array)
                {
                    var itemCount = contentValue.GetArrayLength();

                    if (fieldSchema.TryGetProperty("minItems", out var minItems))
                    {
                        var min = minItems.GetInt32();
                        if (itemCount < min)
                        {
                            errors.Add($"Field '{fieldName}' has {itemCount} items but requires at least {min}.");
                        }
                    }

                    if (fieldSchema.TryGetProperty("maxItems", out var maxItems))
                    {
                        var max = maxItems.GetInt32();
                        if (itemCount > max)
                        {
                            errors.Add($"Field '{fieldName}' has {itemCount} items but allows at most {max}.");
                        }
                    }

                    // Check constraints on individual array items
                    if (fieldSchema.TryGetProperty("items", out var itemSchema))
                    {
                        var index = 0;
                        foreach (var item in contentValue.EnumerateArray())
                        {
                            ValidateItemConstraints($"{fieldName}[{index}]", itemSchema, item, errors);
                            index++;
                        }
                    }
                }

                // Check string length constraints (maxLength, minLength)
                if (contentValue.ValueKind == JsonValueKind.String)
                {
                    var strValue = contentValue.GetString() ?? string.Empty;

                    if (fieldSchema.TryGetProperty("maxLength", out var maxLength))
                    {
                        var max = maxLength.GetInt32();
                        if (strValue.Length > max)
                        {
                            errors.Add($"Field '{fieldName}' is {strValue.Length} characters but maximum is {max}.");
                        }
                    }

                    if (fieldSchema.TryGetProperty("minLength", out var minLength))
                    {
                        var min = minLength.GetInt32();
                        if (strValue.Length < min)
                        {
                            errors.Add($"Field '{fieldName}' is {strValue.Length} characters but minimum is {min}.");
                        }
                    }

                    // Check enum values
                    if (fieldSchema.TryGetProperty("enum", out var enumValues) &&
                        enumValues.ValueKind == JsonValueKind.Array)
                    {
                        var allowed = new List<string>();
                        foreach (var e in enumValues.EnumerateArray())
                        {
                            var val = e.GetString();
                            if (val != null) allowed.Add(val);
                        }

                        if (!allowed.Contains(strValue))
                        {
                            errors.Add($"Field '{fieldName}' has value '{strValue}' but must be one of: {string.Join(", ", allowed)}.");
                        }
                    }
                }
            }
        }

        var isValid = errors.Count == 0;
        return Task.FromResult((isValid, errors));
    }

    /// <summary>
    /// Validates that a content field matches the expected type from the schema.
    /// </summary>
    private static void ValidateFieldType(string fieldName, JsonElement fieldSchema, JsonElement contentValue, List<string> errors)
    {
        if (!fieldSchema.TryGetProperty("type", out var typeElement))
            return;

        var expectedType = typeElement.GetString();

        switch (expectedType)
        {
            case "string":
                if (contentValue.ValueKind != JsonValueKind.String)
                {
                    errors.Add($"Field '{fieldName}' should be a string but is {contentValue.ValueKind}.");
                }
                else if (string.IsNullOrWhiteSpace(contentValue.GetString()))
                {
                    errors.Add($"Field '{fieldName}' is a required string but is empty.");
                }
                break;

            case "array":
                if (contentValue.ValueKind != JsonValueKind.Array)
                {
                    errors.Add($"Field '{fieldName}' should be an array but is {contentValue.ValueKind}.");
                }
                else if (contentValue.GetArrayLength() == 0)
                {
                    errors.Add($"Field '{fieldName}' is a required array but has no elements.");
                }
                break;

            case "object":
                if (contentValue.ValueKind != JsonValueKind.Object)
                {
                    errors.Add($"Field '{fieldName}' should be an object but is {contentValue.ValueKind}.");
                }
                break;

            case "number":
            case "integer":
                if (contentValue.ValueKind != JsonValueKind.Number)
                {
                    errors.Add($"Field '{fieldName}' should be a number but is {contentValue.ValueKind}.");
                }
                break;

            case "boolean":
                if (contentValue.ValueKind != JsonValueKind.True && contentValue.ValueKind != JsonValueKind.False)
                {
                    errors.Add($"Field '{fieldName}' should be a boolean but is {contentValue.ValueKind}.");
                }
                break;
        }
    }

    /// <summary>
    /// Validates constraints on individual items within an array (e.g., nested object property lengths).
    /// </summary>
    private static void ValidateItemConstraints(string path, JsonElement itemSchema, JsonElement item, List<string> errors)
    {
        // If the item schema defines properties (i.e., items are objects), validate nested constraints
        if (itemSchema.TryGetProperty("properties", out var nestedProps))
        {
            if (item.ValueKind != JsonValueKind.Object)
                return;

            foreach (var prop in nestedProps.EnumerateObject())
            {
                var propName = prop.Name;
                if (!item.TryGetProperty(propName, out var propValue))
                    continue;

                var propSchema = prop.Value;

                // String length constraints on nested properties
                if (propValue.ValueKind == JsonValueKind.String)
                {
                    var strValue = propValue.GetString() ?? string.Empty;

                    if (propSchema.TryGetProperty("maxLength", out var maxLength))
                    {
                        var max = maxLength.GetInt32();
                        if (strValue.Length > max)
                        {
                            errors.Add($"Field '{path}.{propName}' is {strValue.Length} characters but maximum is {max}.");
                        }
                    }

                    if (propSchema.TryGetProperty("minLength", out var minLength))
                    {
                        var min = minLength.GetInt32();
                        if (strValue.Length < min)
                        {
                            errors.Add($"Field '{path}.{propName}' is {strValue.Length} characters but minimum is {min}.");
                        }
                    }

                    // Enum constraint on nested properties
                    if (propSchema.TryGetProperty("enum", out var enumValues) &&
                        enumValues.ValueKind == JsonValueKind.Array)
                    {
                        var allowed = new List<string>();
                        foreach (var e in enumValues.EnumerateArray())
                        {
                            var val = e.GetString();
                            if (val != null) allowed.Add(val);
                        }

                        if (!allowed.Contains(strValue))
                        {
                            errors.Add($"Field '{path}.{propName}' has value '{strValue}' but must be one of: {string.Join(", ", allowed)}.");
                        }
                    }
                }
            }
        }

        // If the item schema defines maxLength directly (i.e., items are strings)
        if (item.ValueKind == JsonValueKind.String)
        {
            var strValue = item.GetString() ?? string.Empty;

            if (itemSchema.TryGetProperty("maxLength", out var maxLength))
            {
                var max = maxLength.GetInt32();
                if (strValue.Length > max)
                {
                    errors.Add($"Item at '{path}' is {strValue.Length} characters but maximum is {max}.");
                }
            }
        }
    }
}
