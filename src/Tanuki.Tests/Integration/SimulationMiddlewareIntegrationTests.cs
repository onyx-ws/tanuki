extern alias Tanuki;

using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Onyx.Tanuki; // Extension methods now in Runtime
using Onyx.Tanuki.Configuration; // For TanukiOptions from Runtime
using Xunit;

namespace Onyx.Tanuki.Tests.Integration;

/// <summary>
/// Integration tests for SimulationMiddleware
/// </summary>
public class SimulationMiddlewareIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private HttpClient? _client;

    public SimulationMiddlewareIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient Client => _client ??= _factory.CreateClient();

    [Fact]
    public async Task GetRequest_ReturnsConfiguredResponse()
    {
        // Act
        var response = await Client.GetAsync("/api/v0.1/ping");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Hello World", content);
    }

    [Fact]
    public async Task GetRequest_WithRandomParameter_ReturnsRandomExample()
    {
        // Act
        var response1 = await Client.GetAsync("/api/v0.1/ping?random");
        var response2 = await Client.GetAsync("/api/v0.1/ping?random");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        // At least one should succeed (may be same or different)
        // Note: External values may not be fetched in test environment, so content might be empty
        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();
        // At least one response should have content (either inline or external)
        Assert.True(!string.IsNullOrEmpty(content1) || !string.IsNullOrEmpty(content2), 
            "At least one random response should have content");
    }

    [Fact]
    public async Task GetRequest_WithExampleParameter_ReturnsSpecificExample()
    {
        // Act
        var response = await Client.GetAsync("/api/v0.1/ping?example=reply-1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Hello World Coded!", content);
    }

    [Fact]
    public async Task GetRequest_WithStatusParameter_ReturnsSpecifiedStatusCode()
    {
        // Act
        var response = await Client.GetAsync("/api/v0.1/ping?status=500");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("error", content);
    }

    [Fact]
    public async Task GetRequest_WithAcceptHeader_ReturnsMatchingContentType()
    {
        // Act - Request XML
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v0.1/data");
        request.Headers.Accept.ParseAdd("application/xml");
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("<data>", content);
    }

    [Fact]
    public async Task GetRequest_WithJsonAcceptHeader_ReturnsJson()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v0.1/data");
        request.Headers.Accept.ParseAdd("application/json");
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"data\"", content);
    }

    [Fact]
    public async Task PostRequest_ReturnsCreatedStatusCode()
    {
        // Act
        var response = await Client.PostAsync("/api/v0.1/ping", null);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("created", content);
    }

    [Fact]
    public async Task Request_NonExistentPath_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync("/api/nonexistent");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Request_WrongMethod_ReturnsMethodNotAllowed()
    {
        // Act - DELETE is not configured for /api/v0.1/ping
        var response = await Client.DeleteAsync("/api/v0.1/ping");

        // Assert
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

/// <summary>
/// Factory for creating test web application
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Tanuki::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set test environment to skip async initialization during discovery
        builder.UseEnvironment("Testing");
        
        // Configure content root to point to Tanuki project directory
        var tanukiProjectDir = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "Tanuki"));
        
        builder.UseContentRoot(tanukiProjectDir);
        
        builder.ConfigureServices((context, services) =>
        {
            // Override configuration file path for testing
            var configPath = System.IO.Path.Combine(tanukiProjectDir, "tanuki.json");
            
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException(
                    $"Test configuration file not found at: {configPath}. " +
                    $"Base directory: {AppContext.BaseDirectory}");
            }
            
            services.Configure<TanukiOptions>(options =>
            {
                options.ConfigurationFilePath = configPath;
            });
        });
    }
}

