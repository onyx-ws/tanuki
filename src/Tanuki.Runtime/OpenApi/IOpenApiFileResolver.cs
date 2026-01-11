namespace Onyx.Tanuki.OpenApi;

/// <summary>
/// Resolves OpenAPI file paths from directories or files.
/// </summary>
public interface IOpenApiFileResolver
{
    /// <summary>
    /// Resolves OpenAPI file path from either a directory or file path.
    /// If directory, searches for openapi.yaml, openapi.yml, or openapi.json.
    /// </summary>
    /// <param name="path">Path to file or directory.</param>
    /// <returns>The resolved file path.</returns>
    /// <exception cref="ArgumentException">If path is null or empty.</exception>
    /// <exception cref="FileNotFoundException">If file or directory doesn't exist, or no OpenAPI file found in directory.</exception>
    string ResolveFile(string path);

    /// <summary>
    /// Resolves base directory for external reference resolution.
    /// </summary>
    /// <param name="filePath">Path to the OpenAPI file.</param>
    /// <param name="baseDirectory">Optional explicit base directory.</param>
    /// <returns>The resolved base directory.</returns>
    string ResolveBaseDirectory(string filePath, string? baseDirectory);
}
