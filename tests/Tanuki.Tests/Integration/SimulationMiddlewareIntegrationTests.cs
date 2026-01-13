using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Onyx.Tanuki;
using Onyx.Tanuki.Configuration;
using Onyx.Tanuki.Constants;
using Xunit;

namespace Onyx.Tanuki.Tests.Integration;

/// <summary>
/// Integration tests for SimulationMiddleware
/// </summary>
public class SimulationMiddlewareIntegrationTests : IDisposable
{
    private readonly TestServer _server;
    private readonly HttpClient _client;

    public SimulationMiddlewareIntegrationTests()
    {
        var testDataDir = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "Config"));
        
        var configPath = System.IO.Path.Combine(testDataDir, "tanuki.json");

        // Ensure directory exists
        if (!Directory.Exists(testDataDir))
        {
            Directory.CreateDirectory(testDataDir);
        }

        // If config file doesn't exist, create a dummy one for tests if needed, 
        // or assume it was copied correctly.
        // For now, we assume it exists as per previous steps.

        var builder = new WebHostBuilder()
            .UseEnvironment("Testing")
            .ConfigureAppConfiguration((context, config) => 
            {
                if (File.Exists(configPath))
                {
                    config.AddJsonFile(configPath, optional: false);
                }
            })
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(logging => 
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                });
                
                services.AddTanuki(context.Configuration);
                
                // Ensure configuration file path is set correctly in options
                services.Configure<TanukiOptions>(options =>
                {
                    options.ConfigurationFilePath = configPath;
                });
            })
            .Configure(app =>
            {
                app.UseTanukiExceptionHandling();
                app.UseHealthChecks(TanukiConstants.HealthCheckPath);
                app.UseSimulator();
            });

        _server = new TestServer(builder);
        _client = _server.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _server.Dispose();
    }

    private HttpClient Client => _client;

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
        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();
        Assert.True(!string.IsNullOrEmpty(content1) || !string.IsNullOrEmpty(content2));
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
        // Act - Request XML (assuming configured in tanuki.json, usually it's /api/v0.1/data in examples)
        // Checking previous file content, /api/v0.1/data supports XML
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
    public async Task GetRequest_NonExistentPath_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync("/api/nonexistent");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRequest_UnconfiguredMethod_ReturnsMethodNotAllowed()
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
