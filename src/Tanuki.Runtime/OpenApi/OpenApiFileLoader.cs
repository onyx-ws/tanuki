namespace Onyx.Tanuki.OpenApi;

/// <summary>
/// Implementation of <see cref="IOpenApiFileLoader"/> that loads files and enforces size limits.
/// </summary>
public class OpenApiFileLoader : IOpenApiFileLoader
{
    private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB

    /// <inheritdoc />
    public Task<Stream> LoadFileStreamAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null, empty, or whitespace.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}", filePath);
        }

        var fileInfo = new FileInfo(filePath);
        
        // Check file size
        if (fileInfo.Length > MaxFileSizeBytes)
        {
            var errors = new[]
            {
                new OpenApiErrorInfo
                {
                    Message = $"File size ({fileInfo.Length} bytes) exceeds maximum allowed size ({MaxFileSizeBytes} bytes)."
                }
            };
            throw new OpenApiParseException(filePath, errors);
        }

        // Return file stream
        return Task.FromResult<Stream>(File.OpenRead(filePath));
    }

    /// <inheritdoc />
    public FileInfo GetFileInfo(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null, empty, or whitespace.", nameof(filePath));
        }

        return new FileInfo(filePath);
    }
}
