using Microsoft.OpenApi;

namespace Onyx.Tanuki.OpenApi;

/// <summary>
/// Result of parsing an OpenAPI document.
/// </summary>
public class OpenApiParseResult
{
    /// <summary>
    /// The parsed OpenAPI document (null if parsing failed).
    /// </summary>
    public OpenApiDocument? Document { get; set; }

    /// <summary>
    /// List of errors encountered during parsing.
    /// </summary>
    public IReadOnlyList<OpenApiErrorInfo> Errors { get; set; } = Array.Empty<OpenApiErrorInfo>();

    /// <summary>
    /// List of warnings encountered during parsing (if any).
    /// </summary>
    public IReadOnlyList<OpenApiErrorInfo>? Warnings { get; set; }

    /// <summary>
    /// Indicates whether parsing was successful.
    /// </summary>
    public bool IsSuccess => Document != null && !Errors.Any();
}
