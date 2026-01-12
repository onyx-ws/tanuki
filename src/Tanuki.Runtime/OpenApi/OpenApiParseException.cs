using System.Text.Json;

namespace Onyx.Tanuki.OpenApi;

/// <summary>
/// Exception thrown when OpenAPI document parsing fails.
/// </summary>
public class OpenApiParseException : Exception
{
    /// <summary>
    /// The path to the OpenAPI file that failed to parse.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// List of errors encountered during parsing.
    /// </summary>
    public IReadOnlyList<OpenApiErrorInfo> Errors { get; }

    /// <summary>
    /// List of warnings encountered during parsing (if any).
    /// </summary>
    public IReadOnlyList<OpenApiErrorInfo>? Warnings { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiParseException"/> class.
    /// </summary>
    /// <param name="filePath">The path to the OpenAPI file that failed to parse.</param>
    /// <param name="errors">List of errors encountered during parsing.</param>
    /// <param name="warnings">Optional list of warnings encountered during parsing.</param>
    public OpenApiParseException(
        string filePath,
        IReadOnlyList<OpenApiErrorInfo> errors,
        IReadOnlyList<OpenApiErrorInfo>? warnings = null)
        : base(BuildMessage(filePath, errors))
    {
        FilePath = filePath;
        Errors = errors;
        Warnings = warnings;
    }

    private static string BuildMessage(string filePath, IReadOnlyList<OpenApiErrorInfo> errors)
    {
        var errorMessages = string.Join(", ", errors.Select(e => e.Message));
        return $"Failed to parse OpenAPI file '{filePath}': {errorMessages}";
    }

    /// <summary>
    /// Converts the exception to a JSON representation for machine parsing.
    /// </summary>
    /// <returns>A JSON string representation of the exception.</returns>
    public string ToJson()
    {
        var json = new
        {
            valid = false,
            errors = Errors.Select(e => new
            {
                message = e.Message,
                pointer = e.Pointer,
                line = e.Line,
                column = e.Column
            }).ToArray(),
            warnings = Warnings?.Select(w => new
            {
                message = w.Message,
                pointer = w.Pointer,
                line = w.Line,
                column = w.Column
            }).ToArray()
        };

        return JsonSerializer.Serialize(json, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
