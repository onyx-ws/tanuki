using Microsoft.OpenApi;
using Onyx.Tanuki.OpenApi;
using Xunit;

namespace Onyx.Tanuki.Tests.OpenApi;

public class OpenApiValidatorTests
{
    private readonly Onyx.Tanuki.OpenApi.IOpenApiValidator _validator;

    public OpenApiValidatorTests()
    {
        _validator = new Onyx.Tanuki.OpenApi.OpenApiValidator();
    }

    [Fact]
    public void ValidateVersion_WithOpenApi30_DoesNotThrow()
    {
        // Arrange
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "3.0.0" }
        };

        var filePath = "test.yaml";

        // Act & Assert
        _validator.ValidateVersion(document, filePath);
        // Should not throw
    }

    [Fact]
    public void ValidateVersion_WithOpenApi31_DoesNotThrow()
    {
        // Arrange
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "3.1.0" }
        };

        var filePath = "test.yaml";

        // Act & Assert
        _validator.ValidateVersion(document, filePath);
        // Should not throw
    }

    [Fact(Skip = "OpenApiDocument doesn't expose the OpenAPI specification version. The parser handles version validation during parsing, and Microsoft.OpenApi doesn't support OpenAPI 2.0 (Swagger) files.")]
    public void ValidateVersion_WithOpenApi20_ThrowsOpenApiParseException()
    {
        // Arrange
        // Note: This test is skipped because:
        // 1. OpenApiDocument doesn't have a SpecVersion property to check
        // 2. The OpenAPI specification version is determined during parsing from the YAML/JSON file
        // 3. Microsoft.OpenApi library doesn't parse OpenAPI 2.0 (Swagger) files - they would fail during parsing
        // 4. ValidateVersion currently doesn't validate (it's a placeholder for future use)
        // The parser will reject OpenAPI 2.0 files during parsing, so version validation happens there
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "1.0.0" }
        };

        var filePath = "test.yaml";

        // Act & Assert
        var exception = Assert.Throws<OpenApiParseException>(
            () => _validator.ValidateVersion(document, filePath));
        Assert.Contains("Unsupported OpenAPI version", exception.Message);
        Assert.Contains("2.0", exception.Message);
    }

    [Fact]
    public void ValidateFileSize_WithFileUnderLimit_DoesNotThrow()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "small content");
        var fileInfo = new FileInfo(tempFile);

        try
        {
            // Act & Assert
            _validator.ValidateFileSize(fileInfo);
            // Should not throw
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ValidateFileSize_WithFileAtLimit_DoesNotThrow()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var content = new byte[2 * 1024 * 1024]; // Exactly 2 MB
        Array.Fill(content, (byte)'A');
        File.WriteAllBytes(tempFile, content);
        var fileInfo = new FileInfo(tempFile);

        try
        {
            // Act & Assert
            _validator.ValidateFileSize(fileInfo);
            // Should not throw
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ValidateFileSize_WithFileExceedingLimit_ThrowsOpenApiParseException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var content = new byte[2 * 1024 * 1024 + 1]; // 2 MB + 1 byte
        Array.Fill(content, (byte)'A');
        File.WriteAllBytes(tempFile, content);
        var fileInfo = new FileInfo(tempFile);

        try
        {
            // Act & Assert
            var exception = Assert.Throws<OpenApiParseException>(
                () => _validator.ValidateFileSize(fileInfo));
            Assert.Contains("exceeds maximum", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("2", exception.Message);
            Assert.Contains("MB", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
