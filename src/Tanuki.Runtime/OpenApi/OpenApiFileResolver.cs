namespace Onyx.Tanuki.OpenApi;

/// <summary>
/// Implementation of <see cref="IOpenApiFileResolver"/> that resolves OpenAPI file paths.
/// </summary>
public class OpenApiFileResolver : IOpenApiFileResolver
{
    private static readonly string[] OpenApiFileNames = { "openapi.yaml", "openapi.yml", "openapi.json" };

    /// <inheritdoc />
    public string ResolveFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null, empty, or whitespace.", nameof(path));
        }

        // Check if it's a file
        if (File.Exists(path))
        {
            return path;
        }

        // Check if it's a directory
        if (Directory.Exists(path))
        {
            // Search for OpenAPI files in priority order: openapi.yaml, openapi.yml, openapi.json
            foreach (var fileName in OpenApiFileNames)
            {
                var filePath = Path.Combine(path, fileName);
                if (File.Exists(filePath))
                {
                    return filePath;
                }
            }

            throw new FileNotFoundException($"No OpenAPI file found in directory '{path}'. Expected one of: {string.Join(", ", OpenApiFileNames)}");
        }

        // Neither file nor directory exists
        throw new FileNotFoundException($"File or directory not found: {path}");
    }

    /// <inheritdoc />
    public string ResolveBaseDirectory(string filePath, string? baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null, empty, or whitespace.", nameof(filePath));
        }

        // If explicit base directory is provided, use it
        if (!string.IsNullOrWhiteSpace(baseDirectory))
        {
            return Path.GetFullPath(baseDirectory);
        }

        // Otherwise, use the directory containing the file
        var fullFilePath = Path.GetFullPath(filePath);
        var fileDir = Path.GetDirectoryName(fullFilePath);
        
        if (string.IsNullOrEmpty(fileDir))
        {
            // If file path has no directory (e.g., just a filename), use current directory
            return Directory.GetCurrentDirectory();
        }

        return fileDir;
    }
}
