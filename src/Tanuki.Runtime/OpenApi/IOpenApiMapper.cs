using Microsoft.OpenApi;
using Onyx.Tanuki.Configuration;

namespace Onyx.Tanuki.OpenApi;

/// <summary>
/// Maps OpenAPI documents to Tanuki configuration.
/// </summary>
public interface IOpenApiMapper
{
    /// <summary>
    /// Maps an OpenAPI document to a Tanuki configuration.
    /// </summary>
    /// <param name="document">The OpenAPI document to map.</param>
    /// <returns>The mapped Tanuki configuration.</returns>
    /// <exception cref="ArgumentNullException">If document is null.</exception>
    Configuration.Tanuki Map(OpenApiDocument document);
}
