using Microsoft.OpenApi;

namespace Onyx.Tanuki.OpenApi;

/// <summary>
/// Loads and parses OpenAPI documents from files, resolving external references.
/// This is a facade that orchestrates file resolution, loading, parsing, and validation.
/// </summary>
public interface IOpenApiDocumentLoader
{
    /// <summary>
    /// Loads an OpenAPI document from a file path or directory, resolving external references.
    /// </summary>
    /// <param name="path">Path to the OpenAPI file (JSON or YAML) or directory containing openapi.yaml/yml/json.</param>
    /// <param name="baseDirectory">Optional base directory for resolving relative external references.
    ///                              If null, uses the file's directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed OpenAPI document.</returns>
    /// <exception cref="OpenApiParseException">If parsing fails or document is invalid.</exception>
    /// <exception cref="FileNotFoundException">If the file doesn't exist.</exception>
    Task<OpenApiDocument> LoadAsync(
        string path,
        string? baseDirectory = null,
        CancellationToken cancellationToken = default);
}
