using Onyx.Tanuki.OpenApi;
using Xunit;

namespace Onyx.Tanuki.Tests.OpenApi;

public class OpenApiFileLoaderTests
{
    private readonly IOpenApiFileLoader _loader;

    public OpenApiFileLoaderTests()
    {
        _loader = new OpenApiFileLoader();
    }

    [Fact]
    public async Task LoadFileStreamAsync_WithExistingFile_ReturnsStream()
    {
        // Arrange - Use real OpenAPI sample file
        var filePath = TestDataHelper.PetstoreYaml;
        Assert.True(File.Exists(filePath), $"Test data file not found: {filePath}");
        var expectedContent = await File.ReadAllTextAsync(filePath);

        // Act
        await using var stream = await _loader.LoadFileStreamAsync(filePath);

        // Assert
        Assert.NotNull(stream);
        Assert.True(stream.CanRead);
        using var reader = new StreamReader(stream);
        var streamContent = await reader.ReadToEndAsync();
        Assert.Equal(expectedContent, streamContent);
    }

    [Fact]
    public async Task LoadFileStreamAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".yaml");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _loader.LoadFileStreamAsync(nonExistentFile));
    }

    [Fact]
    public async Task LoadFileStreamAsync_WithFileExceedingSizeLimit_ThrowsOpenApiParseException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        // Create a file larger than 2 MB
        var largeContent = new byte[2 * 1024 * 1024 + 1]; // 2 MB + 1 byte
        Array.Fill(largeContent, (byte)'A');
        File.WriteAllBytes(tempFile, largeContent);

        try
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<OpenApiParseException>(
                () => _loader.LoadFileStreamAsync(tempFile));
            Assert.Contains("exceeds maximum", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadFileStreamAsync_WithFileAtSizeLimit_ReturnsStream()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        // Create a file exactly at 2 MB
        var content = new byte[2 * 1024 * 1024]; // Exactly 2 MB
        Array.Fill(content, (byte)'A');
        File.WriteAllBytes(tempFile, content);

        try
        {
            // Act
            await using var stream = await _loader.LoadFileStreamAsync(tempFile);

            // Assert
            Assert.NotNull(stream);
            Assert.True(stream.CanRead);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadFileStreamAsync_WithSmallFile_ReturnsStream()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "small content");

        try
        {
            // Act
            await using var stream = await _loader.LoadFileStreamAsync(tempFile);

            // Assert
            Assert.NotNull(stream);
            Assert.True(stream.CanRead);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetFileInfo_WithExistingFile_ReturnsFileInfo()
    {
        // Arrange - Use real OpenAPI sample file
        var filePath = TestDataHelper.PetstoreYaml;
        Assert.True(File.Exists(filePath), $"Test data file not found: {filePath}");

        // Act
        var fileInfo = _loader.GetFileInfo(filePath);

        // Assert
        Assert.NotNull(fileInfo);
        Assert.True(fileInfo.Exists);
        Assert.Equal(Path.GetFullPath(filePath), fileInfo.FullName);
        Assert.True(fileInfo.Length > 0, "File should have content");
    }

    [Fact]
    public void GetFileInfo_WithNonExistentFile_ReturnsFileInfoWithExistsFalse()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".yaml");

        // Act
        var fileInfo = _loader.GetFileInfo(nonExistentFile);

        // Assert
        Assert.NotNull(fileInfo);
        Assert.False(fileInfo.Exists);
    }
}
