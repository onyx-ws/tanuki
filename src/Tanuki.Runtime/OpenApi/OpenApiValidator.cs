using Microsoft.OpenApi;

namespace Onyx.Tanuki.OpenApi;

/// <summary>
/// Implementation of <see cref="IOpenApiValidator"/> that validates OpenAPI documents.
/// </summary>
public class OpenApiValidator : IOpenApiValidator
{
    private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB

    /// <inheritdoc />
    public void ValidateVersion(OpenApiDocument document, string filePath)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null, empty, or whitespace.", nameof(filePath));
        }

        // Note: OpenAPI spec version validation is handled by the parser during parsing.
        // The parser will reject documents that don't have a valid OpenAPI spec version (3.0.x, 3.1.x, etc.).
        // This method is kept for potential future validation of API version constraints if needed.
        // For now, we skip validation here since the parser already validates the spec version.
    }

    /// <inheritdoc />
    public void ValidateFileSize(FileInfo fileInfo)
    {
        if (fileInfo == null)
        {
            throw new ArgumentNullException(nameof(fileInfo));
        }

        if (fileInfo.Length > MaxFileSizeBytes)
        {
            var errors = new[]
            {
                new OpenApiErrorInfo
                {
                    Message = $"File size ({fileInfo.Length} bytes) exceeds maximum allowed size (2 MB / {MaxFileSizeBytes} bytes)."
                }
            };
            throw new OpenApiParseException(fileInfo.FullName, errors);
        }
    }
}
