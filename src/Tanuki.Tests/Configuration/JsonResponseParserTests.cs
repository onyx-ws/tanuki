extern alias Tanuki;

using System.Text.Json;
using Tanuki::Onyx.Tanuki.Configuration;
using Tanuki::Onyx.Tanuki.Configuration.Exceptions;
using Tanuki::Onyx.Tanuki.Configuration.Json;
using Xunit;

namespace Onyx.Tanuki.Tests.Configuration;

public class JsonResponseParserTests
{
    [Fact]
    public void Parse_ValidResponse_ReturnsResponse()
    {
        // Arrange
        var json = JsonDocument.Parse("""
        {
          "200": {
            "description": "Success",
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
        """).RootElement.EnumerateObject().First();

        // Act
        var result = JsonResponseParser.Parse(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("200", result.StatusCode);
        Assert.Equal("Success", result.Description);
        Assert.Single(result.Content);
    }

    [Fact]
    public void Parse_InvalidStatusCode_ThrowsException()
    {
        // Arrange
        var json = JsonDocument.Parse("""
        {
          "999": {
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
        """).RootElement.EnumerateObject().First();

        // Act & Assert
        var exception = Assert.Throws<TanukiConfigurationException>(() => JsonResponseParser.Parse(json));
        Assert.Contains("Invalid response status code", exception.Message);
    }

    [Fact]
    public void Parse_EmptyStatusCode_ThrowsException()
    {
        // Arrange
        var json = JsonDocument.Parse("""
        {
          "": {
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
        """).RootElement.EnumerateObject().First();

        // Act & Assert
        var exception = Assert.Throws<TanukiConfigurationException>(() => JsonResponseParser.Parse(json));
        Assert.Contains("cannot be empty", exception.Message);
    }

    [Fact]
    public void Parse_MissingContent_ThrowsException()
    {
        // Arrange
        var json = JsonDocument.Parse("""
        {
          "200": {
            "description": "Success"
          }
        }
        """).RootElement.EnumerateObject().First();

        // Act & Assert
        var exception = Assert.Throws<TanukiConfigurationException>(() => JsonResponseParser.Parse(json));
        Assert.Contains("content", exception.Message);
    }
}
