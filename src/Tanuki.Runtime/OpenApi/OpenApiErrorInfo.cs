namespace Onyx.Tanuki.OpenApi;

/// <summary>
/// Represents an error or warning information from OpenAPI parsing.
/// </summary>
public class OpenApiErrorInfo
{
    /// <summary>
    /// The error or warning message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// JSON pointer to the location in the document where the error occurred (e.g., "/paths/~1users/get").
    /// </summary>
    public string? Pointer { get; set; }

    /// <summary>
    /// Line number where the error occurred (if available).
    /// </summary>
    public int? Line { get; set; }

    /// <summary>
    /// Column number where the error occurred (if available).
    /// </summary>
    public int? Column { get; set; }
}
