using Microsoft.OpenApi;

namespace Onyx.Tanuki.OpenApi;

/// <summary>
/// Generates deterministic examples from OpenAPI schemas.
/// </summary>
public interface IOpenApiExampleGenerator
{
    /// <summary>
    /// Generates an example value from an OpenAPI schema.
    /// Returns null if schema is null or if generation is not supported.
    /// </summary>
    /// <param name="schema">The OpenAPI schema to generate an example from.</param>
    /// <param name="document">The OpenAPI document (for resolving $ref references).</param>
    /// <returns>A JSON string representation of the generated example, or null if generation is not supported.</returns>
    string? GenerateExample(IOpenApiSchema? schema, OpenApiDocument document);
}
