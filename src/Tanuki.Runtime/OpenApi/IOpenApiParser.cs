namespace Onyx.Tanuki.OpenApi;

/// <summary>
/// Parses OpenAPI documents from streams.
/// </summary>
public interface IOpenApiParser
{
    /// <summary>
    /// Parses an OpenAPI document from a stream.
    /// </summary>
    /// <param name="stream">Stream containing the OpenAPI document.</param>
    /// <param name="format">Format of the document (JSON or YAML).</param>
    /// <param name="baseDirectory">Base directory for resolving external references.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parse result containing the document and any errors/warnings.</returns>
    Task<OpenApiParseResult> ParseAsync(
        Stream stream,
        OpenApiFormat format,
        string? baseDirectory = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// File format enumeration.
/// </summary>
public enum OpenApiFormat
{
    Json,
    Yaml
}
