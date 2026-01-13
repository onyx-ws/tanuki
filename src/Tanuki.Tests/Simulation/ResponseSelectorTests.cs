extern alias Tanuki;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using Onyx.Tanuki.Configuration;
using Xunit;
using IResponseSelector = Tanuki::Onyx.Tanuki.Simulation.IResponseSelector;
using ResponseSelector = Tanuki::Onyx.Tanuki.Simulation.ResponseSelector;

namespace Onyx.Tanuki.Tests.Simulation;

public class ResponseSelectorTests
{
    private readonly IResponseSelector _selector;
    private readonly Mock<HttpContext> _mockContext;
    private readonly Mock<HttpRequest> _mockRequest;
    private readonly Mock<IQueryCollection> _mockQuery;

    public ResponseSelectorTests()
    {
        _selector = new ResponseSelector();
        _mockContext = new Mock<HttpContext>();
        _mockRequest = new Mock<HttpRequest>();
        _mockQuery = new Mock<IQueryCollection>();
        
        _mockContext.Setup(c => c.Request).Returns(_mockRequest.Object);
        _mockRequest.Setup(r => r.Query).Returns(_mockQuery.Object);
    }

    [Fact]
    public void SelectResponse_NoQueryParams_ReturnsFirstResponse()
    {
        // Arrange
        var operation = new Operation
        {
            Name = "GET",
            Responses = new List<Response>
            {
                new() { StatusCode = "200" },
                new() { StatusCode = "404" }
            }
        };

        _mockQuery.Setup(q => q.TryGetValue("status", out It.Ref<StringValues>.IsAny))
            .Returns(false);

        // Act
        var result = _selector.SelectResponse(operation, _mockContext.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("200", result.StatusCode);
    }

    [Fact]
    public void SelectResponse_WithStatusQuery_ReturnsMatchingResponse()
    {
        // Arrange
        var operation = new Operation
        {
            Name = "GET",
            Responses = new List<Response>
            {
                new() { StatusCode = "200" },
                new() { StatusCode = "404" }
            }
        };

        var statusValue = new StringValues("404");
        _mockQuery.Setup(q => q.TryGetValue("status", out It.Ref<StringValues>.IsAny))
            .Returns((string key, out StringValues value) =>
            {
                value = statusValue;
                return true;
            });

        // Act
        var result = _selector.SelectResponse(operation, _mockContext.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("404", result.StatusCode);
    }

    [Fact]
    public void SelectContent_WithAcceptHeader_ReturnsMatchingContent()
    {
        // Arrange
        var response = new Response
        {
            StatusCode = "200",
            Content = new List<Content>
            {
                new() { MediaType = "application/json" },
                new() { MediaType = "application/xml" }
            }
        };

        var mockHeaders = new Mock<IHeaderDictionary>();
        mockHeaders.Setup(h => h["Accept"]).Returns(new StringValues("application/json"));
        _mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);

        // Act
        var result = _selector.SelectContent(response, _mockContext.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("application/json", result.MediaType);
    }

    [Fact]
    public void SelectExample_WithExampleQuery_ReturnsMatchingExample()
    {
        // Arrange
        var content = new Content
        {
            MediaType = "application/json",
            Examples = new List<Example>
            {
                new() { Name = "example1", Value = "{}" },
                new() { Name = "example2", Value = "[]" }
            }
        };

        var exampleValue = new StringValues("example2");
        _mockQuery.Setup(q => q.TryGetValue("example", out It.Ref<StringValues>.IsAny))
            .Returns((string key, out StringValues value) =>
            {
                value = exampleValue;
                return true;
            });

        // Act
        var result = _selector.SelectExample(content, _mockContext.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("example2", result.Name);
    }

    [Fact]
    public void SelectExample_WithRandomQuery_ReturnsRandomExample()
    {
        // Arrange
        var content = new Content
        {
            MediaType = "application/json",
            Examples = new List<Example>
            {
                new() { Name = "example1", Value = "{}" },
                new() { Name = "example2", Value = "[]" },
                new() { Name = "example3", Value = "\"test\"" }
            }
        };

        _mockQuery.Setup(q => q.TryGetValue("example", out It.Ref<StringValues>.IsAny))
            .Returns(false);
        _mockQuery.Setup(q => q.ContainsKey("random")).Returns(true);

        // Act
        var result = _selector.SelectExample(content, _mockContext.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(content.Examples, e => e.Name == result.Name);
    }
}
