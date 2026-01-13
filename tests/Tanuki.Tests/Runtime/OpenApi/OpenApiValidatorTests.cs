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

    [Fact]
    public void ValidateVersion_WithOpenApi20_DoesNotThrow()
    {
        // Arrange
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "1.0.0" }
            // Note: OpenApiDocument is version-agnostic (it's the v3 model).
            // The parser converts v2 to this model.
            // Since the parser succeeds for v2 (as verified by tests),
            // the validator should just accept the document object.
        };

        var filePath = "test.yaml";

        // Act & Assert
        _validator.ValidateVersion(document, filePath);
        // Should not throw
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
