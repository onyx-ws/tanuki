namespace Onyx.Tanuki.OpenApi;

/// <summary>
/// Loads OpenAPI files and handles file I/O operations.
/// </summary>
public interface IOpenApiFileLoader
{
    /// <summary>
    /// Loads a file stream for the given path, checking size limits.
    /// </summary>
    /// <param name="filePath">Path to the file to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A stream for reading the file.</returns>
    /// <exception cref="FileNotFoundException">If the file doesn't exist.</exception>
    /// <exception cref="OpenApiParseException">If file size exceeds limits.</exception>
    Task<Stream> LoadFileStreamAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file information (size, exists, etc.).
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <returns>File information.</returns>
    FileInfo GetFileInfo(string filePath);
}
