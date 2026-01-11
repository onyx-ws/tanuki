using Onyx.Tanuki.OpenApi;
using Xunit;

namespace Onyx.Tanuki.Tests.OpenApi;

public class OpenApiFileResolverTests
{
    private readonly IOpenApiFileResolver _resolver;

    public OpenApiFileResolverTests()
    {
        _resolver = new OpenApiFileResolver();
    }

    [Fact]
    public void ResolveFile_WithExistingFile_ReturnsFilePath()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "test");

            // Act
            var result = _resolver.ResolveFile(tempFile);

            // Assert
            Assert.Equal(tempFile, result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ResolveFile_WithDirectoryContainingOpenApiYaml_ReturnsFilePath()
    {
        // Arrange - Use test data directory which contains openapi.yaml
        var testDataDir = TestDataHelper.TestDataDirectory;
        Assert.True(Directory.Exists(testDataDir), $"Test data directory not found: {testDataDir}");
        var expectedFile = Path.GetFullPath(Path.Combine(testDataDir, "openapi.yaml"));
        Assert.True(File.Exists(expectedFile), $"Expected file not found: {expectedFile}");

        // Act
        var result = _resolver.ResolveFile(testDataDir);

        // Assert
        Assert.Equal(expectedFile, Path.GetFullPath(result));
    }

    [Fact]
    public void ResolveFile_WithDirectoryContainingOpenApiYml_ReturnsFilePath()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var expectedFile = Path.Combine(tempDir, "openapi.yml");
        File.WriteAllText(expectedFile, "openapi: 3.0.0");

        try
        {
            // Act
            var result = _resolver.ResolveFile(tempDir);

            // Assert
            Assert.Equal(expectedFile, result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ResolveFile_WithDirectoryContainingOpenApiJson_ReturnsFilePath()
    {
        // Arrange - Create temp directory with only JSON file
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var expectedFile = Path.Combine(tempDir, "openapi.json");
        // Copy test data file
        File.Copy(TestDataHelper.PetstoreJson, expectedFile);

        try
        {
            // Act
            var result = _resolver.ResolveFile(tempDir);

            // Assert
            Assert.Equal(expectedFile, result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ResolveFile_WithDirectoryContainingMultipleFiles_PrefersYaml()
    {
        // Arrange - Use test data directory which has both files
        var testDataDir = TestDataHelper.TestDataDirectory;
        Assert.True(Directory.Exists(testDataDir), $"Test data directory not found: {testDataDir}");
        var yamlFile = Path.GetFullPath(Path.Combine(testDataDir, "openapi.yaml"));
        var jsonFile = Path.GetFullPath(Path.Combine(testDataDir, "openapi.json"));
        Assert.True(File.Exists(yamlFile), $"YAML file not found: {yamlFile}");
        Assert.True(File.Exists(jsonFile), $"JSON file not found: {jsonFile}");

        // Act
        var result = _resolver.ResolveFile(testDataDir);

        // Assert - Should prefer YAML
        Assert.Equal(yamlFile, Path.GetFullPath(result));
    }

    [Fact]
    public void ResolveFile_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".yaml");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _resolver.ResolveFile(nonExistentFile));
    }

    [Fact]
    public void ResolveFile_WithEmptyDirectory_ThrowsFileNotFoundException()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act & Assert
            var exception = Assert.Throws<FileNotFoundException>(() => _resolver.ResolveFile(tempDir));
            Assert.Contains("No OpenAPI file found", exception.Message);
        }
        finally
        {
            Directory.Delete(tempDir);
        }
    }

    [Fact]
    public void ResolveFile_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _resolver.ResolveFile(null!));
    }

    [Fact]
    public void ResolveFile_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _resolver.ResolveFile(string.Empty));
    }

    [Fact]
    public void ResolveFile_WithWhitespacePath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _resolver.ResolveFile("   "));
    }

    [Fact]
    public void ResolveBaseDirectory_WithExplicitBaseDirectory_ReturnsExplicitDirectory()
    {
        // Arrange
        var filePath = Path.Combine(Path.GetTempPath(), "test.yaml");
        var baseDir = Path.Combine(Path.GetTempPath(), "base");

        // Act
        var result = _resolver.ResolveBaseDirectory(filePath, baseDir);

        // Assert
        Assert.Equal(Path.GetFullPath(baseDir), result);
    }

    [Fact]
    public void ResolveBaseDirectory_WithNullBaseDirectory_ReturnsFileDirectory()
    {
        // Arrange
        var filePath = Path.Combine(Path.GetTempPath(), "test.yaml");
        var expectedDir = Path.GetDirectoryName(Path.GetFullPath(filePath));

        // Act
        var result = _resolver.ResolveBaseDirectory(filePath, null);

        // Assert
        Assert.Equal(expectedDir, result);
    }

    [Fact]
    public void ResolveBaseDirectory_WithRelativePath_ResolvesToAbsolutePath()
    {
        // Arrange
        var filePath = "./test.yaml";
        var baseDir = "./base";

        // Act
        var result = _resolver.ResolveBaseDirectory(filePath, baseDir);

        // Assert
        Assert.True(Path.IsPathRooted(result));
    }
}
