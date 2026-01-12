using Microsoft.OpenApi;

namespace Onyx.Tanuki.OpenApi;

/// <summary>
/// Validates OpenAPI documents and file constraints.
/// </summary>
public interface IOpenApiValidator
{
    /// <summary>
    /// Validates the OpenAPI document version (must be 3.0.x or 3.1.x).
    /// </summary>
    /// <param name="document">The parsed OpenAPI document.</param>
    /// <param name="filePath">Path to the file (for error reporting).</param>
    /// <exception cref="OpenApiParseException">If version is not supported.</exception>
    void ValidateVersion(OpenApiDocument document, string filePath);

    /// <summary>
    /// Validates file size against limits.
    /// </summary>
    /// <param name="fileInfo">File information.</param>
    /// <exception cref="OpenApiParseException">If file size exceeds limits.</exception>
    void ValidateFileSize(FileInfo fileInfo);
}
