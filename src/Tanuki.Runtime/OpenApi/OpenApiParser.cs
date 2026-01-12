using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Onyx.Tanuki.OpenApi;

/// <summary>
/// Implementation of <see cref="IOpenApiParser"/> that parses OpenAPI documents using Microsoft.OpenApi.
/// </summary>
public class OpenApiParser : IOpenApiParser
{
    private readonly ILogger<OpenApiParser>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiParser"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    public OpenApiParser(ILogger<OpenApiParser>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<OpenApiParseResult> ParseAsync(
        Stream stream,
        OpenApiFormat format,
        string? baseDirectory = null,
        CancellationToken cancellationToken = default)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        try
        {
            // Reset stream position if possible
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            // Configure reader settings - explicitly add YAML reader support
            // Reference: Working sample code that successfully handles both YAML and JSON
            var settings = new OpenApiReaderSettings { LeaveStreamOpen = false };
            settings.AddYamlReader();
            
            if (!string.IsNullOrEmpty(baseDirectory))
            {
                // Use Uri constructor to properly handle file paths cross-platform
                // This ensures correct URI format: file:///C:/path on Windows, file:///path on Unix/Linux
                // The Uri class automatically handles the three-slash format (file:///) required for absolute paths
                var basePath = Path.GetFullPath(baseDirectory);
                settings.BaseUrl = new Uri(basePath, UriKind.Absolute);
            }
            
            // Use OpenApiDocument.LoadAsync with stream - the library auto-detects format from stream content
            // (JSON vs YAML is detected by inspecting the stream content)
            // Reference: Working sample code that successfully handles both YAML and JSON
            (var document, var diagnostic) = await OpenApiDocument.LoadAsync(stream, settings: settings);
            
            if (document == null)
            {
                return new OpenApiParseResult
                {
                    Document = null,
                    Errors = new[]
                    {
                        new OpenApiErrorInfo
                        {
                            Message = "Failed to parse OpenAPI document: document is null."
                        }
                    }
                };
            }
            
            // Map diagnostics
            var warnings = diagnostic.Warnings.Any()
                ? diagnostic.Warnings.Select(e => MapDiagnosticError(e)).ToList()
                : null;
            
            // Check for errors in diagnostics
            if (diagnostic.Errors.Any())
            {
                var errorInfos = diagnostic.Errors
                    .Select(e => MapDiagnosticError(e))
                    .ToList();
                
                return new OpenApiParseResult
                {
                    Document = null,
                    Errors = errorInfos,
                    Warnings = warnings
                };
            }

            // Log warnings if logger is available
            if (warnings != null && warnings.Any() && _logger != null)
            {
                foreach (var warning in warnings)
                {
                    _logger.LogWarning("OpenAPI parsing warning: {Message} at {Pointer}", warning.Message, warning.Pointer);
                }
            }

            return new OpenApiParseResult
            {
                Document = document,
                Errors = Array.Empty<OpenApiErrorInfo>(),
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            // Handle unexpected exceptions
            _logger?.LogError(ex, "Unexpected error during OpenAPI parsing");
            
            return new OpenApiParseResult
            {
                Document = null,
                Errors = new[]
                {
                    new OpenApiErrorInfo
                    {
                        Message = $"Unexpected error during parsing: {ex.Message}"
                    }
                }
            };
        }
    }

    /// <summary>
    /// Maps a diagnostic error from OpenAPI.NET to OpenApiErrorInfo.
    /// </summary>
    private static OpenApiErrorInfo MapDiagnosticError(OpenApiError error)
    {
        return new OpenApiErrorInfo
        {
            Message = error.Message ?? "Unknown error",
            Pointer = error.Pointer
        };
    }
}
