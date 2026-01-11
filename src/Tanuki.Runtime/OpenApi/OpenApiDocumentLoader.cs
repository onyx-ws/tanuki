using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace Onyx.Tanuki.OpenApi;

/// <summary>
/// Implementation of <see cref="IOpenApiDocumentLoader"/> that orchestrates file resolution,
/// loading, parsing, and validation to load OpenAPI documents.
/// </summary>
public class OpenApiDocumentLoader : IOpenApiDocumentLoader
{
    private readonly IOpenApiFileResolver _fileResolver;
    private readonly IOpenApiFileLoader _fileLoader;
    private readonly IOpenApiParser _parser;
    private readonly IOpenApiValidator _validator;
    private readonly ILogger<OpenApiDocumentLoader>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiDocumentLoader"/> class.
    /// </summary>
    /// <param name="fileResolver">File resolver for resolving paths.</param>
    /// <param name="fileLoader">File loader for loading file streams.</param>
    /// <param name="parser">Parser for parsing OpenAPI documents.</param>
    /// <param name="validator">Validator for validating documents.</param>
    /// <param name="logger">Optional logger for warnings and diagnostic information.</param>
    public OpenApiDocumentLoader(
        IOpenApiFileResolver fileResolver,
        IOpenApiFileLoader fileLoader,
        IOpenApiParser parser,
        IOpenApiValidator validator,
        ILogger<OpenApiDocumentLoader>? logger = null)
    {
        _fileResolver = fileResolver ?? throw new ArgumentNullException(nameof(fileResolver));
        _fileLoader = fileLoader ?? throw new ArgumentNullException(nameof(fileLoader));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<OpenApiDocument> LoadAsync(
        string path,
        string? baseDirectory = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null, empty, or whitespace.", nameof(path));
        }

        // 1. Resolve file path (directory → file)
        var resolvedPath = _fileResolver.ResolveFile(path);

        // 2. Resolve base directory
        var resolvedBaseDirectory = _fileResolver.ResolveBaseDirectory(resolvedPath, baseDirectory);

        // 3. Load file stream (fileLoader handles size check)
        await using var fileStream = await _fileLoader.LoadFileStreamAsync(resolvedPath, cancellationToken);

        // 4. Detect format from file extension
        var format = DetectFormat(resolvedPath);

        // 5. Parse document
        var parseResult = await _parser.ParseAsync(fileStream, format, resolvedBaseDirectory, cancellationToken);

        // 6. Handle errors/warnings from parsing
        if (!parseResult.IsSuccess)
        {
            var errors = parseResult.Errors.ToList();
            var warnings = parseResult.Warnings?.ToList();
            throw new OpenApiParseException(resolvedPath, errors, warnings);
        }

        if (parseResult.Document == null)
        {
            var errors = new[]
            {
                new OpenApiErrorInfo
                {
                    Message = "Failed to parse OpenAPI document: document is null."
                }
            };
            throw new OpenApiParseException(resolvedPath, errors);
        }

        // 7. Validate version
        _validator.ValidateVersion(parseResult.Document, resolvedPath);

        // 8. Return document
        return parseResult.Document;
    }

    /// <summary>
    /// Detects the file format (JSON or YAML) based on file extension.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <returns>The detected format.</returns>
    /// <exception cref="ArgumentException">If file extension is not supported.</exception>
    private static OpenApiFormat DetectFormat(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null, empty, or whitespace.", nameof(filePath));
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".json" => OpenApiFormat.Json,
            ".yaml" or ".yml" => OpenApiFormat.Yaml,
            _ => throw new ArgumentException(
                $"Unsupported file extension: {extension}. Supported extensions: .json, .yaml, .yml",
                nameof(filePath))
        };
    }
}
