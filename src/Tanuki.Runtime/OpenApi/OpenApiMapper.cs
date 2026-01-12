using System.Globalization;
using Microsoft.OpenApi;
using Onyx.Tanuki.Configuration;

namespace Onyx.Tanuki.OpenApi;

/// <summary>
/// Maps OpenAPI documents to Tanuki configuration.
/// </summary>
public class OpenApiMapper : IOpenApiMapper
{
    private readonly IOpenApiExampleGenerator _exampleGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiMapper"/> class.
    /// </summary>
    /// <param name="exampleGenerator">The example generator to use when no examples are present.</param>
    public OpenApiMapper(IOpenApiExampleGenerator? exampleGenerator = null)
    {
        _exampleGenerator = exampleGenerator ?? new OpenApiExampleGenerator();
    }

    /// <inheritdoc />
    public Configuration.Tanuki Map(OpenApiDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var tanuki = new Configuration.Tanuki();

        if (document.Paths == null || !document.Paths.Any())
        {
            return tanuki;
        }

        foreach (var pathItem in document.Paths)
        {
            var path = MapPath(pathItem.Key, pathItem.Value, document);
            if (path != null)
            {
                tanuki.Paths.Add(path);
            }
        }

        return tanuki;
    }

    private Configuration.Path? MapPath(string uri, IOpenApiPathItem pathItem, OpenApiDocument document)
    {
        if (pathItem == null)
        {
            return null;
        }

        var path = new Configuration.Path
        {
            Uri = uri
        };

        // Map operations (GET, POST, PUT, DELETE, etc.)
        if (pathItem.Operations != null)
        {
            foreach (var operation in pathItem.Operations)
            {
                var mappedOperation = MapOperation(operation.Key.ToString(), operation.Value, document);
                if (mappedOperation != null)
                {
                    path.Operations.Add(mappedOperation);
                }
            }
        }

        // Only return path if it has operations
        return path.Operations.Any() ? path : null;
    }

    private Configuration.Operation? MapOperation(string operationTypeName, dynamic openApiOperation, OpenApiDocument document)
    {
        if (openApiOperation == null)
        {
            return null;
        }

        var operation = new Configuration.Operation
        {
            Name = operationTypeName.ToUpperInvariant(),
            Summary = GetProperty(openApiOperation, "Summary") ?? string.Empty,
            Description = GetProperty(openApiOperation, "Description") ?? string.Empty,
            OperationId = GetProperty(openApiOperation, "OperationId") ?? string.Empty,
            Tags = GetTags(openApiOperation)
        };

        // Map responses
        if (openApiOperation.Responses != null)
        {
            foreach (var response in openApiOperation.Responses)
            {
                var mappedResponse = MapResponse(response.Key, response.Value, document);
                if (mappedResponse != null)
                {
                    operation.Responses.Add(mappedResponse);
                }
            }
        }

        return operation;
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

    private List<string> GetTags(dynamic openApiOperation)
    {
        try
        {
            var tags = openApiOperation.Tags;
            if (tags == null)
            {
                return new List<string>();
            }

            var result = new List<string>();
            foreach (var tag in tags)
            {
                var name = tag?.Name;
                if (!string.IsNullOrEmpty(name))
                {
                    result.Add(name);
                }
            }
            return result;
        }
        catch
        {
            return new List<string>();
        }
    }

    private Configuration.Response? MapResponse(string statusCode, IOpenApiResponse openApiResponse, OpenApiDocument document)
    {
        if (openApiResponse == null)
        {
            return null;
        }

        var response = new Configuration.Response
        {
            StatusCode = statusCode,
            Description = openApiResponse.Description ?? string.Empty
        };

        // Map content
        if (openApiResponse.Content != null)
        {
            foreach (var content in openApiResponse.Content)
            {
                var mappedContent = MapContent(content.Key, content.Value, document);
                if (mappedContent != null)
                {
                    response.Content.Add(mappedContent);
                }
            }
        }

        return response;
    }

    private Configuration.Content? MapContent(string mediaType, IOpenApiMediaType openApiMediaType, OpenApiDocument document)
    {
        if (openApiMediaType == null)
        {
            return null;
        }

        var content = new Configuration.Content
        {
            MediaType = mediaType
        };

        // Map examples
        if (openApiMediaType.Examples != null && openApiMediaType.Examples.Any())
        {
            foreach (var example in openApiMediaType.Examples)
            {
                var mappedExample = MapExample(example.Key, example.Value);
                if (mappedExample != null)
                {
                    content.Examples.Add(mappedExample);
                }
            }
        }
        // If no examples but there's a single example property, map it
        else if (openApiMediaType.Example != null)
        {
            // Create a default example from the single example value
            var defaultExample = new Configuration.Example
            {
                Name = "default",
                Value = SerializeExample(openApiMediaType.Example)
            };
            content.Examples.Add(defaultExample);
        }
        // If no examples at all, try to generate one from the schema
        else
        {
            // Access schema using dynamic (similar to other property access)
            dynamic dynamicMediaType = openApiMediaType;
            var schemaProperty = dynamicMediaType.GetType().GetProperty("Schema");
            if (schemaProperty != null)
            {
                var schema = schemaProperty.GetValue(dynamicMediaType);
                if (schema != null)
                {
                    // Generate example from schema
                    var generatedExample = _exampleGenerator.GenerateExample((IOpenApiSchema)schema, document);
                    if (generatedExample != null)
                    {
                        var defaultExample = new Configuration.Example
                        {
                            Name = "default",
                            Value = generatedExample
                        };
                        content.Examples.Add(defaultExample);
                    }
                }
            }
        }

        return content;
    }

    private Configuration.Example? MapExample(string name, IOpenApiExample openApiExample)
    {
        if (openApiExample == null)
        {
            return null;
        }

        var example = new Configuration.Example
        {
            Name = name,
            Summary = openApiExample.Summary ?? string.Empty,
            Description = openApiExample.Description ?? string.Empty,
            ExternalValue = openApiExample.ExternalValue?.ToString()
        };

        // Map value if present
        if (openApiExample.Value != null)
        {
            example.Value = SerializeExample(openApiExample.Value);
        }

        return example;
    }

    private string SerializeExample(dynamic value)
    {
        // Convert OpenAPI Any type to JSON string
        // This is a simple implementation - may need enhancement for complex types
        try
        {
            // Try to get the value property for simple types
            if (value != null)
            {
                var valueType = value.GetType();
                var valueProperty = valueType.GetProperty("Value");
                if (valueProperty != null)
                {
                    var actualValue = valueProperty.GetValue(value);
                    if (actualValue != null)
                    {
                        // For strings, JSON-encode them (add quotes) since Example.Value must be valid JSON
                        if (actualValue is string str)
                        {
                            return System.Text.Json.JsonSerializer.Serialize(str);
                        }
                        // For numbers, convert to string using invariant culture to ensure valid JSON
                        if (actualValue is int || actualValue is long || actualValue is double || actualValue is float || actualValue is decimal)
                        {
                            return actualValue.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                        }
                        // For booleans, convert to lowercase string
                        if (actualValue is bool boolVal)
                        {
                            return boolVal.ToString().ToLowerInvariant();
                        }
                        // For complex types, serialize to JSON
                        return System.Text.Json.JsonSerializer.Serialize(actualValue);
                    }
                }
            }
        }
        catch
        {
            // If reflection fails, try direct serialization
        }

        // Fallback: try to serialize the whole object
        try
        {
            // Check if value is a string before serializing
            if (value is string strValue)
            {
                return System.Text.Json.JsonSerializer.Serialize(strValue);
            }
            return System.Text.Json.JsonSerializer.Serialize(value);
        }
        catch
        {
            // Final fallback: if value is a string, JSON-encode it; otherwise use ToString()
            if (value is string finalStrValue)
            {
                return System.Text.Json.JsonSerializer.Serialize(finalStrValue);
            }
            // For numeric types, use invariant culture to ensure valid JSON
            if (value is int || value is long || value is double || value is float || value is decimal)
            {
                return value.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            }
            return value?.ToString() ?? string.Empty;
        }
    }
}
