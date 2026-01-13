using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.OpenApi;
using Onyx.Tanuki.OpenApi;
using Xunit;

namespace Onyx.Tanuki.Tests.OpenApi;

public class OpenApiExampleGeneratorTests
{
    private readonly IOpenApiExampleGenerator _generator;
    private readonly IOpenApiDocumentLoader _documentLoader;

    public OpenApiExampleGeneratorTests()
    {
        _generator = new OpenApiExampleGenerator();
        
        // Create a real document loader for integration tests
        var fileResolver = new OpenApiFileResolver();
        var fileLoader = new OpenApiFileLoader();
        var logger = new Mock<ILogger<OpenApiParser>>();
        var parser = new OpenApiParser(logger.Object);
        var validator = new Onyx.Tanuki.OpenApi.OpenApiValidator();
        var loaderLogger = new Mock<ILogger<OpenApiDocumentLoader>>();
        _documentLoader = new OpenApiDocumentLoader(fileResolver, fileLoader, parser, validator, loaderLogger.Object);
    }

    // Helper method to create a minimal OpenAPI document
    private OpenApiDocument CreateMinimalDocument()
    {
        return new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "Test API",
                Version = "1.0.0"
            }
        };
    }

    [Fact]
    public void GenerateExample_WithNullSchema_ReturnsNull()
    {
        // Arrange
        var document = CreateMinimalDocument();

        // Act
        var result = _generator.GenerateExample(null, document);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateExample_WithStringSchema_ReturnsString()
    {
        // Arrange - Create OpenAPI document with string schema
        var tempFile = Path.GetTempFileName() + ".json";
        try
        {
            var json = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""string""
                }
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
            
            // Extract schema from the document
            var operation = document.Paths["/test"].Operations.First().Value; // Get first operation (GET)
            var schema = operation.Responses["200"].Content["application/json"].Schema;

            // Act
            var result = _generator.GenerateExample(schema, document);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("\"string\"", result);
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
    public async Task GenerateExample_WithIntegerSchema_ReturnsZero()
    {
        // Arrange
        var tempFile = Path.GetTempFileName() + ".json";
        try
        {
            var json = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""integer""
                }
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
            var operation = document.Paths["/test"].Operations.First().Value;
            var schema = operation.Responses["200"].Content["application/json"].Schema;

            // Act
            var result = _generator.GenerateExample(schema, document);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("0", result);
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
    public async Task GenerateExample_WithNumberSchema_ReturnsZero()
    {
        // Arrange
        var tempFile = Path.GetTempFileName() + ".json";
        try
        {
            var json = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""number""
                }
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
            var operation = document.Paths["/test"].Operations.First().Value;
            var schema = operation.Responses["200"].Content["application/json"].Schema;

            // Act
            var result = _generator.GenerateExample(schema, document);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("0.0", result);
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
    public async Task GenerateExample_WithBooleanSchema_ReturnsFalse()
    {
        // Arrange
        var tempFile = Path.GetTempFileName() + ".json";
        try
        {
            var json = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""boolean""
                }
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
            var operation = document.Paths["/test"].Operations.First().Value;
            var schema = operation.Responses["200"].Content["application/json"].Schema;

            // Act
            var result = _generator.GenerateExample(schema, document);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("false", result);
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
    public async Task GenerateExample_WithArraySchemaWithoutItems_ReturnsEmptyArray()
    {
        // Arrange
        var tempFile = Path.GetTempFileName() + ".json";
        try
        {
            var json = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""array""
                }
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
            var operation = document.Paths["/test"].Operations.First().Value;
            var schema = operation.Responses["200"].Content["application/json"].Schema;

            // Act
            var result = _generator.GenerateExample(schema, document);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("[]", result);
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
    public async Task GenerateExample_WithArraySchemaWithStringItems_ReturnsArrayWithOneString()
    {
        // Arrange
        var tempFile = Path.GetTempFileName() + ".json";
        try
        {
            var json = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""array"",
                  ""items"": {
                    ""type"": ""string""
                  }
                }
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
            var operation = document.Paths["/test"].Operations.First().Value;
            var schema = operation.Responses["200"].Content["application/json"].Schema;

            // Act
            var result = _generator.GenerateExample(schema, document);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("[\"string\"]", result);
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
    public async Task GenerateExample_WithObjectSchemaWithoutProperties_ReturnsEmptyObject()
    {
        // Arrange
        var tempFile = Path.GetTempFileName() + ".json";
        try
        {
            var json = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""object""
                }
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
            var operation = document.Paths["/test"].Operations.First().Value;
            var schema = operation.Responses["200"].Content["application/json"].Schema;

            // Act
            var result = _generator.GenerateExample(schema, document);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("{}", result);
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
    public async Task GenerateExample_WithObjectSchemaWithProperties_ReturnsObjectWithProperties()
    {
        // Arrange
        var tempFile = Path.GetTempFileName() + ".json";
        try
        {
            var json = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""object"",
                  ""properties"": {
                    ""name"": {
                      ""type"": ""string""
                    },
                    ""age"": {
                      ""type"": ""integer""
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
}";
            await File.WriteAllTextAsync(tempFile, json);
            var document = await _documentLoader.LoadAsync(tempFile);
            var operation = document.Paths["/test"].Operations.First().Value;
            var schema = operation.Responses["200"].Content["application/json"].Schema;

            // Act
            var result = _generator.GenerateExample(schema, document);

            // Assert
            Assert.NotNull(result);
            // Parse JSON to verify structure
            var jsonDoc = System.Text.Json.JsonDocument.Parse(result);
            Assert.True(jsonDoc.RootElement.TryGetProperty("name", out var nameProp));
            Assert.Equal("string", nameProp.GetString()); // GetString() returns the parsed value, not the JSON string
            Assert.True(jsonDoc.RootElement.TryGetProperty("age", out var ageProp));
            Assert.Equal(0, ageProp.GetInt32());
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
    public async Task GenerateExample_WithSchemaReference_ResolvesReference()
    {
        // Arrange - Create OpenAPI document with schema reference
        var tempFile = Path.GetTempFileName() + ".json";
        try
        {
            var json = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/User""
                }
              }
            }
          }
        }
      }
    }
  },
  ""components"": {
    ""schemas"": {
      ""User"": {
        ""type"": ""object"",
        ""properties"": {
          ""name"": {
            ""type"": ""string""
          },
          ""age"": {
            ""type"": ""integer""
          }
        }
      }
    }
  }
}";
            await File.WriteAllTextAsync(tempFile, json);
            var document = await _documentLoader.LoadAsync(tempFile);
            var operation = document.Paths["/test"].Operations.First().Value;
            var schema = operation.Responses["200"].Content["application/json"].Schema;

            // Act
            var result = _generator.GenerateExample(schema, document);

            // Assert
            Assert.NotNull(result);
            // Parse JSON to verify structure
            var jsonDoc = System.Text.Json.JsonDocument.Parse(result);
            Assert.True(jsonDoc.RootElement.TryGetProperty("name", out var nameProp));
            Assert.Equal("string", nameProp.GetString());
            Assert.True(jsonDoc.RootElement.TryGetProperty("age", out var ageProp));
            Assert.Equal(0, ageProp.GetInt32());
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
    public async Task GenerateExample_WithObjectSchemaWithSpecialCharacterPropertyNames_EscapesPropertyNames()
    {
        // Arrange - Test property names with quotes, backslashes, and control characters
        var tempFile = Path.GetTempFileName() + ".json";
        try
        {
            var json = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""object"",
                  ""properties"": {
                    ""test\""name"": {
                      ""type"": ""string""
                    },
                    ""test\\backslash"": {
                      ""type"": ""integer""
                    },
                    ""test\nnewline"": {
                      ""type"": ""boolean""
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
}";
            await File.WriteAllTextAsync(tempFile, json);
            var document = await _documentLoader.LoadAsync(tempFile);
            var operation = document.Paths["/test"].Operations.First().Value;
            var schema = operation.Responses["200"].Content["application/json"].Schema;

            // Act
            var result = _generator.GenerateExample(schema, document);

            // Assert
            Assert.NotNull(result);
            // Verify the JSON is valid and can be parsed
            var jsonDoc = System.Text.Json.JsonDocument.Parse(result);
            Assert.True(jsonDoc.RootElement.TryGetProperty("test\"name", out var quoteProp));
            Assert.Equal("string", quoteProp.GetString());
            Assert.True(jsonDoc.RootElement.TryGetProperty("test\\backslash", out var backslashProp));
            Assert.Equal(0, backslashProp.GetInt32());
            Assert.True(jsonDoc.RootElement.TryGetProperty("test\nnewline", out var newlineProp));
            Assert.False(newlineProp.GetBoolean());
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
