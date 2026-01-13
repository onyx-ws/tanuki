extern alias Tanuki;

using Tanuki::Onyx.Tanuki.Configuration;
using Onyx.Tanuki.Configuration.Exceptions;
using Onyx.Tanuki.Configuration.Json;
using Xunit;

namespace Onyx.Tanuki.Tests.Json;

public class JsonConfigParserTests
{
    [Fact]
    public void Parse_ValidJson_ReturnsTanuki()
    {
        // Arrange
        var json = """
        {
          "paths": {
            "/api/test": {
              "get": {
                "responses": {
                  "200": {
                    "content": {
                      "application/json": {
                        "examples": {
                          "test": {
                            "value": "{}"
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
        """;

        // Act
        var result = JsonConfigParser.Parse(json);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Paths);
        Assert.Equal("/api/test", result.Paths[0].Uri);
    }

    [Fact]
    public void Parse_EmptyJson_ThrowsException()
    {
        // Arrange
        var json = "";

        // Act & Assert
        var exception = Assert.Throws<TanukiConfigurationException>(() => JsonConfigParser.Parse(json));
        Assert.Contains("null or empty", exception.Message);
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsException()
    {
        // Arrange
        var json = "{ invalid json }";

        // Act & Assert
        var exception = Assert.Throws<TanukiConfigurationException>(() => JsonConfigParser.Parse(json));
        Assert.Contains("Invalid JSON format", exception.Message);
    }

    [Fact]
    public void Parse_MissingPaths_ThrowsException()
    {
        // Arrange
        var json = "{}";

        // Act & Assert
        var exception = Assert.Throws<TanukiConfigurationException>(() => JsonConfigParser.Parse(json));
        Assert.Contains("paths", exception.Message);
    }

    [Fact]
    public void Parse_WithComments_HandlesComments()
    {
        // Arrange
        var json = """
        {
          // This is a comment
          "paths": {
            "/api/test": {
              "get": {
                "responses": {
                  "200": {
                    "content": {
                      "application/json": {
                        "examples": {
                          "test": {
                            "value": "{}"
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
        """;

        // Act
        var result = JsonConfigParser.Parse(json);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Paths);
    }
}
