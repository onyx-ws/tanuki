using Microsoft.Extensions.Logging;
using Moq;
using Onyx.Tanuki.Configuration;
using Onyx.Tanuki.OpenApi;
using Xunit;

namespace Onyx.Tanuki.Tests.OpenApi;

public class OpenApiMapperTests
{
    private readonly IOpenApiMapper _mapper;
    private readonly IOpenApiDocumentLoader _documentLoader;

    public OpenApiMapperTests()
    {
        _mapper = new OpenApiMapper();
        
        // Create a real document loader for integration tests
        var fileResolver = new OpenApiFileResolver();
        var fileLoader = new OpenApiFileLoader();
        var logger = new Mock<ILogger<OpenApiParser>>();
        var parser = new OpenApiParser(logger.Object);
        var validator = new OpenApiValidator();
        var loaderLogger = new Mock<ILogger<OpenApiDocumentLoader>>();
        _documentLoader = new OpenApiDocumentLoader(fileResolver, fileLoader, parser, validator, loaderLogger.Object);
    }

    [Fact]
    public void Map_WithNullDocument_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _mapper.Map(null!));
    }

    [Fact]
    public async Task Map_WithEmptyDocument_ReturnsEmptyTanukiConfig()
    {
        // Arrange - Create a minimal valid OpenAPI document
        var tempFile = System.IO.Path.GetTempFileName() + ".json";
        try
        {
            var minimalJson = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {}
}";
            await File.WriteAllTextAsync(tempFile, minimalJson);
            
            var document = await _documentLoader.LoadAsync(tempFile);

            // Act
            var result = _mapper.Map(document);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Paths);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task Map_WithSinglePathAndGetOperation_MapsCorrectly()
    {
        // Arrange - Use a real OpenAPI document
        var tempFile = System.IO.Path.GetTempFileName() + ".json";
        try
        {
            var json = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/ping"": {
      ""get"": {
        ""summary"": ""Ping endpoint"",
        ""operationId"": ""ping"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""example"": ""pong""
              }
            }
          }
        }
      }
    }
  }
}";
            await File.WriteAllTextAsync(tempFile, json);
            
            var document = await _documentLoader.LoadAsync(tempFile);

            // Act
            var result = _mapper.Map(document);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Paths);
            var path = result.Paths[0];
            Assert.Equal("/ping", path.Uri);
            Assert.Single(path.Operations);
            var operation = path.Operations[0];
            Assert.Equal("GET", operation.Name);
            Assert.Equal("Ping endpoint", operation.Summary);
            Assert.Equal("ping", operation.OperationId);
            Assert.Single(operation.Responses);
            var response = operation.Responses[0];
            Assert.Equal("200", response.StatusCode);
            Assert.Equal("Success", response.Description);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task Map_WithMultipleOperations_MapsAllOperations()
    {
        // Arrange
        var tempFile = System.IO.Path.GetTempFileName() + ".json";
        try
        {
            var json = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/users"": {
      ""get"": {
        ""operationId"": ""getUsers"",
        ""responses"": {
          ""200"": {
            ""description"": ""OK""
          }
        }
      },
      ""post"": {
        ""operationId"": ""createUser"",
        ""responses"": {
          ""201"": {
            ""description"": ""Created""
          }
        }
      }
    }
  }
}";
            await File.WriteAllTextAsync(tempFile, json);
            
            var document = await _documentLoader.LoadAsync(tempFile);

            // Act
            var result = _mapper.Map(document);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Paths);
            var path = result.Paths[0];
            Assert.Equal(2, path.Operations.Count);
            Assert.Contains(path.Operations, op => op.Name == "GET" && op.OperationId == "getUsers");
            Assert.Contains(path.Operations, op => op.Name == "POST" && op.OperationId == "createUser");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task Map_WithTags_MapsTagsCorrectly()
    {
        // Arrange
        var tempFile = System.IO.Path.GetTempFileName() + ".json";
        try
        {
            var json = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/pets"": {
      ""get"": {
        ""tags"": [""pets"", ""read""],
        ""responses"": {
          ""200"": {
            ""description"": ""OK""
          }
        }
      }
    }
  }
}";
            await File.WriteAllTextAsync(tempFile, json);
            
            var document = await _documentLoader.LoadAsync(tempFile);

            // Act
            var result = _mapper.Map(document);

            // Assert
            Assert.NotNull(result);
            var operation = result.Paths[0].Operations[0];
            Assert.Equal(2, operation.Tags.Count);
            Assert.Contains("pets", operation.Tags);
            Assert.Contains("read", operation.Tags);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task Map_WithRealPetstoreDocument_MapsSuccessfully()
    {
        // Arrange - Use the real Petstore sample
        var filePath = TestDataHelper.PetstoreYaml;
        if (!File.Exists(filePath))
        {
            // Skip if test data not available
            return;
        }

        var document = await _documentLoader.LoadAsync(filePath);

        // Act
        var result = _mapper.Map(document);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Paths);
        // Petstore has multiple paths, verify at least one is mapped
        Assert.True(result.Paths.Count > 0);
    }
}
