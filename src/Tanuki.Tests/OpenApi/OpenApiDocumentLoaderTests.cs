using Microsoft.Extensions.Logging;
using Moq;
using Onyx.Tanuki.OpenApi;
using Xunit;

namespace Onyx.Tanuki.Tests.OpenApi;

public class OpenApiDocumentLoaderTests
{
    private readonly IOpenApiDocumentLoader _loader;
    private readonly Mock<IOpenApiFileResolver> _mockFileResolver;
    private readonly Mock<IOpenApiFileLoader> _mockFileLoader;
    private readonly Mock<IOpenApiParser> _mockParser;
    private readonly Mock<IOpenApiValidator> _mockValidator;

    public OpenApiDocumentLoaderTests()
    {
        _mockFileResolver = new Mock<IOpenApiFileResolver>();
        _mockFileLoader = new Mock<IOpenApiFileLoader>();
        _mockParser = new Mock<IOpenApiParser>();
        _mockValidator = new Mock<IOpenApiValidator>();

        var logger = new Mock<ILogger<OpenApiDocumentLoader>>();
        _loader = new OpenApiDocumentLoader(
            _mockFileResolver.Object,
            _mockFileLoader.Object,
            _mockParser.Object,
            _mockValidator.Object,
            logger.Object);
    }

    [Fact]
    public async Task LoadAsync_WithValidFile_ReturnsDocument()
    {
        // Arrange
        var filePath = "test.yaml";
        var resolvedPath = Path.GetFullPath("test.yaml");
        var baseDir = Path.GetDirectoryName(resolvedPath)!;
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test"));

        _mockFileResolver.Setup(r => r.ResolveFile(filePath)).Returns(resolvedPath);
        _mockFileResolver.Setup(r => r.ResolveBaseDirectory(resolvedPath, null)).Returns(baseDir);
        _mockFileLoader.Setup(l => l.LoadFileStreamAsync(resolvedPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);
        
        var parseResult = new OpenApiParseResult
        {
            Document = new Microsoft.OpenApi.OpenApiDocument
            {
                Info = new Microsoft.OpenApi.OpenApiInfo { Title = "Test", Version = "1.0.0" }
            },
            Errors = Array.Empty<OpenApiErrorInfo>()
        };
        _mockParser.Setup(p => p.ParseAsync(
            It.IsAny<Stream>(),
            It.IsAny<OpenApiFormat>(),
            baseDir,
            It.IsAny<CancellationToken>())).ReturnsAsync(parseResult);

        // Act
        var result = await _loader.LoadAsync(filePath);

        // Assert
        Assert.NotNull(result);
        _mockFileResolver.Verify(r => r.ResolveFile(filePath), Times.Once);
        _mockFileLoader.Verify(l => l.LoadFileStreamAsync(resolvedPath, It.IsAny<CancellationToken>()), Times.Once);
        _mockParser.Verify(p => p.ParseAsync(
            It.IsAny<Stream>(),
            It.IsAny<OpenApiFormat>(),
            baseDir,
            It.IsAny<CancellationToken>()), Times.Once);
        _mockValidator.Verify(v => v.ValidateVersion(It.IsAny<Microsoft.OpenApi.OpenApiDocument>(), resolvedPath), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_WithDirectory_ResolvesToFile()
    {
        // Arrange
        var directoryPath = "./test-dir";
        var resolvedFile = Path.Combine(directoryPath, "openapi.yaml");
        var baseDir = Path.GetDirectoryName(resolvedFile)!;
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test"));

        _mockFileResolver.Setup(r => r.ResolveFile(directoryPath)).Returns(resolvedFile);
        _mockFileResolver.Setup(r => r.ResolveBaseDirectory(resolvedFile, null)).Returns(baseDir);
        _mockFileLoader.Setup(l => l.LoadFileStreamAsync(resolvedFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);
        
        var parseResult = new OpenApiParseResult
        {
            Document = new Microsoft.OpenApi.OpenApiDocument
            {
                Info = new Microsoft.OpenApi.OpenApiInfo { Title = "Test", Version = "1.0.0" }
            },
            Errors = Array.Empty<OpenApiErrorInfo>()
        };
        _mockParser.Setup(p => p.ParseAsync(
            It.IsAny<Stream>(),
            It.IsAny<OpenApiFormat>(),
            baseDir,
            It.IsAny<CancellationToken>())).ReturnsAsync(parseResult);

        // Act
        var result = await _loader.LoadAsync(directoryPath);

        // Assert
        Assert.NotNull(result);
        _mockFileResolver.Verify(r => r.ResolveFile(directoryPath), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_WithExplicitBaseDirectory_UsesExplicitDirectory()
    {
        // Arrange
        var filePath = "test.yaml";
        var resolvedPath = Path.GetFullPath("test.yaml");
        var explicitBaseDir = Path.GetFullPath("./explicit-base");
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test"));

        _mockFileResolver.Setup(r => r.ResolveFile(filePath)).Returns(resolvedPath);
        _mockFileResolver.Setup(r => r.ResolveBaseDirectory(resolvedPath, explicitBaseDir)).Returns(explicitBaseDir);
        _mockFileLoader.Setup(l => l.LoadFileStreamAsync(resolvedPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);
        
        var parseResult = new OpenApiParseResult
        {
            Document = new Microsoft.OpenApi.OpenApiDocument
            {
                Info = new Microsoft.OpenApi.OpenApiInfo { Title = "Test", Version = "1.0.0" }
            },
            Errors = Array.Empty<OpenApiErrorInfo>()
        };
        _mockParser.Setup(p => p.ParseAsync(
            It.IsAny<Stream>(),
            It.IsAny<OpenApiFormat>(),
            explicitBaseDir,
            It.IsAny<CancellationToken>())).ReturnsAsync(parseResult);

        // Act
        var result = await _loader.LoadAsync(filePath, explicitBaseDir);

        // Assert
        Assert.NotNull(result);
        _mockFileResolver.Verify(r => r.ResolveBaseDirectory(resolvedPath, explicitBaseDir), Times.Once);
        _mockParser.Verify(p => p.ParseAsync(
            It.IsAny<Stream>(),
            It.IsAny<OpenApiFormat>(),
            explicitBaseDir,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_WithParseErrors_ThrowsOpenApiParseException()
    {
        // Arrange
        var filePath = "test.yaml";
        var resolvedPath = Path.GetFullPath("test.yaml");
        var baseDir = Path.GetDirectoryName(resolvedPath)!;
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("invalid"));

        _mockFileResolver.Setup(r => r.ResolveFile(filePath)).Returns(resolvedPath);
        _mockFileResolver.Setup(r => r.ResolveBaseDirectory(resolvedPath, null)).Returns(baseDir);
        _mockFileLoader.Setup(l => l.LoadFileStreamAsync(resolvedPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);
        
        var parseResult = new OpenApiParseResult
        {
            Document = null,
            Errors = new[]
            {
                new OpenApiErrorInfo { Message = "Parse error", Pointer = "/openapi" }
            }
        };
        _mockParser.Setup(p => p.ParseAsync(
            It.IsAny<Stream>(),
            It.IsAny<OpenApiFormat>(),
            baseDir,
            It.IsAny<CancellationToken>())).ReturnsAsync(parseResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<OpenApiParseException>(
            () => _loader.LoadAsync(filePath));
        Assert.Equal(resolvedPath, exception.FilePath);
        Assert.Single(exception.Errors);
    }

    [Fact]
    public async Task LoadAsync_WithFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var filePath = "nonexistent.yaml";
        _mockFileResolver.Setup(r => r.ResolveFile(filePath))
            .Throws(new FileNotFoundException("File not found", filePath));

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _loader.LoadAsync(filePath));
    }

    [Fact]
    public async Task LoadAsync_WithFileSizeExceeded_ThrowsOpenApiParseException()
    {
        // Arrange
        var filePath = "test.yaml";
        var resolvedPath = Path.GetFullPath("test.yaml");
        _mockFileResolver.Setup(r => r.ResolveFile(filePath)).Returns(resolvedPath);
        _mockFileLoader.Setup(l => l.LoadFileStreamAsync(resolvedPath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OpenApiParseException(
                resolvedPath,
                new[] { new OpenApiErrorInfo { Message = "File size exceeds limit" } }));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<OpenApiParseException>(
            () => _loader.LoadAsync(filePath));
        Assert.Contains("exceeds", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAsync_WithInvalidVersion_ThrowsOpenApiParseException()
    {
        // Arrange
        var filePath = "test.yaml";
        var resolvedPath = Path.GetFullPath("test.yaml");
        var baseDir = Path.GetDirectoryName(resolvedPath)!;
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test"));

        _mockFileResolver.Setup(r => r.ResolveFile(filePath)).Returns(resolvedPath);
        _mockFileResolver.Setup(r => r.ResolveBaseDirectory(resolvedPath, null)).Returns(baseDir);
        _mockFileLoader.Setup(l => l.LoadFileStreamAsync(resolvedPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);
        
        var parseResult = new OpenApiParseResult
        {
            Document = new Microsoft.OpenApi.OpenApiDocument
            {
                Info = new Microsoft.OpenApi.OpenApiInfo { Title = "Test", Version = "1.0.0" }
            },
            Errors = Array.Empty<OpenApiErrorInfo>()
        };
        _mockParser.Setup(p => p.ParseAsync(
            It.IsAny<Stream>(),
            It.IsAny<OpenApiFormat>(),
            baseDir,
            It.IsAny<CancellationToken>())).ReturnsAsync(parseResult);

        _mockValidator.Setup(v => v.ValidateVersion(
            It.IsAny<Microsoft.OpenApi.OpenApiDocument>(),
            resolvedPath))
            .Throws(new OpenApiParseException(
                resolvedPath,
                new[] { new OpenApiErrorInfo { Message = "Unsupported version" } }));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<OpenApiParseException>(
            () => _loader.LoadAsync(filePath));
        Assert.Contains("Unsupported", exception.Message);
    }
}
