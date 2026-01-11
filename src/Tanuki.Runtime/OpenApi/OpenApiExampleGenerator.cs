using Microsoft.OpenApi;
using System.Text.Json;

namespace Onyx.Tanuki.OpenApi;

/// <summary>
/// Implementation of <see cref="IOpenApiExampleGenerator"/> that generates deterministic examples from OpenAPI schemas.
/// </summary>
public class OpenApiExampleGenerator : IOpenApiExampleGenerator
{
    /// <inheritdoc />
    public string? GenerateExample(IOpenApiSchema? schema, OpenApiDocument document)
    {
        if (schema == null)
        {
            return null;
        }

        // Access schema properties using dynamic (similar to mapper approach)
        dynamic dynamicSchema = schema;
        
        // Get the type property
        var schemaType = GetProperty(dynamicSchema, "Type")?.ToString()?.ToLowerInvariant();
        
        if (string.IsNullOrEmpty(schemaType))
        {
            return null;
        }

        // Generate example based on type
        return schemaType switch
        {
            "string" => "\"string\"",
            "integer" => "0",
            "number" => "0.0",
            "boolean" => "false",
            "array" => GenerateArrayExample(dynamicSchema, document),
            "object" => GenerateObjectExample(dynamicSchema, document),
            _ => null
        };
    }

    private string? GetProperty(dynamic obj, string propertyName)
    {
        try
        {
            var property = obj.GetType().GetProperty(propertyName);
            return property?.GetValue(obj)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private string? GenerateArrayExample(dynamic schema, OpenApiDocument document)
    {
        // Get items schema
        var itemsProperty = schema.GetType().GetProperty("Items");
        if (itemsProperty == null)
        {
            return "[]";
        }

        var itemsSchema = itemsProperty.GetValue(schema);
        if (itemsSchema == null)
        {
            return "[]";
        }

        // Generate example for item schema
        var itemExample = GenerateExample((IOpenApiSchema)itemsSchema, document);
        if (itemExample == null)
        {
            return "[]";
        }

        // Return array with one item
        return $"[{itemExample}]";
    }

    private string? GenerateObjectExample(dynamic schema, OpenApiDocument document)
    {
        // Get properties
        var propertiesProperty = schema.GetType().GetProperty("Properties");
        if (propertiesProperty == null)
        {
            return "{}";
        }

        var properties = propertiesProperty.GetValue(schema);
        if (properties == null)
        {
            return "{}";
        }

        // Build JSON object from properties
        var jsonProperties = new List<string>();

        // Use reflection to iterate through properties dictionary
        if (properties is System.Collections.IDictionary propertiesDict)
        {
            foreach (System.Collections.DictionaryEntry entry in propertiesDict)
            {
                var propertyName = entry.Key.ToString();
                if (propertyName == null)
                {
                    continue;
                }

                var propertySchema = entry.Value;
                if (propertySchema == null)
                {
                    continue;
                }

                // Generate example for property schema
                var propertyExample = GenerateExample((IOpenApiSchema)propertySchema, document);
                if (propertyExample != null)
                {
                    jsonProperties.Add($"\"{propertyName}\": {propertyExample}");
                }
            }
        }

        return jsonProperties.Count > 0
            ? $"{{{string.Join(", ", jsonProperties)}}}"
            : "{}";
    }
}
