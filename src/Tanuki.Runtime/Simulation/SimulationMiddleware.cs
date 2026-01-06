using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Onyx.Tanuki.Configuration;
using Onyx.Tanuki.Constants;
using System.Linq;

namespace Onyx.Tanuki.Simulation;

/// <summary>
/// Middleware that intercepts HTTP requests and returns mock responses based on the Tanuki configuration
/// </summary>
public class SimulationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SimulationMiddleware>? _logger;
    private readonly IResponseSelector _responseSelector;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationMiddleware"/> class
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    /// <param name="responseSelector">The response selector service</param>
    /// <param name="logger">Optional logger instance</param>
    public SimulationMiddleware(
        RequestDelegate next, 
        IResponseSelector responseSelector,
        ILogger<SimulationMiddleware>? logger = null)
    {
        _next = next;
        _responseSelector = responseSelector;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to process the HTTP request
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="configService">The Tanuki configuration service</param>
    public async Task InvokeAsync(HttpContext context, ITanukiConfigurationService configService)
    {
        var requestPath = context.Request.Path;
        
        // Skip simulation for health check endpoints
        if (requestPath.StartsWithSegments(TanukiConstants.HealthCheckPath, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }
        
        var requestMethod = context.Request.Method;
        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger?.LogDebug(
            "Processing request: {Method} {Path} from {RemoteIpAddress}", 
            requestMethod, 
            requestPath,
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown");

        // Find a matching Path using optimized dictionary lookup
        var path = configService.GetPathByUri(requestPath);
        
        if (path is null)
        {
            stopwatch.Stop();
            _logger?.LogWarning(
                "Path not found: {Path} for {Method} | Duration: {Duration}ms", 
                requestPath, 
                requestMethod,
                stopwatch.ElapsedMilliseconds);
            await CreateNotFoundResponse(context);
            return;
        }

        // Path found - Match Operation
        var operation = path.Operations.FirstOrDefault(op =>
            op.Name.Equals(requestMethod, StringComparison.InvariantCultureIgnoreCase));
        
        if (operation is null)
        {
            stopwatch.Stop();
            _logger?.LogWarning(
                "Method not allowed: {Method} on {Path} | Duration: {Duration}ms", 
                requestMethod, 
                requestPath,
                stopwatch.ElapsedMilliseconds);
            await CreateMethodNotAllowedResponse(context);
            return;
        }

        // Apply delay simulation if configured
        var delayMs = 0;
        if (operation.MinDelay.HasValue || operation.MaxDelay.HasValue)
        {
            delayMs = CalculateDelay(operation.MinDelay, operation.MaxDelay);
            if (delayMs > 0)
            {
                _logger?.LogDebug("Simulating delay: {Delay}ms for {Method} {Path}", delayMs, requestMethod, requestPath);
                await Task.Delay(delayMs);
            }
        }

        // Select response, content, and example
        var response = _responseSelector.SelectResponse(operation, context);
        if (response is null)
        {
            stopwatch.Stop();
            _logger?.LogWarning(
                "No response found for operation {OperationId} on {Path} | Duration: {Duration}ms", 
                operation.OperationId, 
                requestPath,
                stopwatch.ElapsedMilliseconds);
            await CreateNotFoundResponse(context);
            return;
        }

        var content = _responseSelector.SelectContent(response, context);
        if (content is null)
        {
            stopwatch.Stop();
            _logger?.LogWarning(
                "No content found for response {StatusCode} on {Path} | Duration: {Duration}ms", 
                response.StatusCode, 
                requestPath,
                stopwatch.ElapsedMilliseconds);
            await CreateNotFoundResponse(context);
            return;
        }

        var example = _responseSelector.SelectExample(content, context);
        if (example is null)
        {
            stopwatch.Stop();
            _logger?.LogWarning(
                "No example found for content {MediaType} on {Path} | Duration: {Duration}ms", 
                content.MediaType, 
                requestPath,
                stopwatch.ElapsedMilliseconds);
            await CreateNotFoundResponse(context);
            return;
        }

        // Set response status code
        if (int.TryParse(response.StatusCode, out var statusCode) && 
            statusCode >= TanukiConstants.MinHttpStatusCode && statusCode < TanukiConstants.MaxHttpStatusCode)
        {
            context.Response.StatusCode = statusCode;
        }
        else
        {
            // This should never happen if configuration validation worked correctly
            _logger?.LogError(
                "Invalid status code '{StatusCode}' for response. This indicates a configuration validation issue. Defaulting to {DefaultStatusCode}.",
                response.StatusCode, TanukiConstants.DefaultHttpStatusCode);
            context.Response.StatusCode = TanukiConstants.DefaultHttpStatusCode;
        }

        // Set content type
        context.Response.ContentType = content.MediaType;

        // Write response body
        // If Value is null but ExternalValue exists, fetch it on-demand (fallback for race conditions)
        string value;
        if (string.IsNullOrWhiteSpace(example.Value) && !string.IsNullOrWhiteSpace(example.ExternalValue))
        {
            _logger?.LogDebug("External value not yet loaded for {Url}, fetching on-demand", example.ExternalValue);
            try
            {
                // Try to get external value fetcher from DI if available
                var fetcher = context.RequestServices.GetService<IExternalValueFetcher>();
                if (fetcher != null)
                {
                    var fetchedValue = await fetcher.FetchAsync(example.ExternalValue, context.RequestAborted);
                    if (fetchedValue != null)
                    {
                        example.Value = fetchedValue;
                    }
                }
                value = example.Value ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to fetch external value on-demand from {Url}, returning empty response", example.ExternalValue);
                value = string.Empty;
            }
        }
        else
        {
            value = example.Value ?? string.Empty;
        }
        
        var responseSize = value.Length;
        await context.Response.WriteAsync(value);

        stopwatch.Stop();
        var totalDuration = stopwatch.ElapsedMilliseconds;
        var processingDuration = totalDuration - delayMs;

        _logger?.LogInformation(
            "Response sent: {StatusCode} {ContentType} for {Method} {Path} | Duration: {TotalDuration}ms (processing: {ProcessingDuration}ms, delay: {DelayMs}ms) | ResponseSize: {ResponseSize} bytes | Example: {ExampleName}", 
            context.Response.StatusCode, 
            content.MediaType, 
            requestMethod, 
            requestPath,
            totalDuration,
            processingDuration,
            delayMs,
            responseSize,
            example.Name);
    }

    private static int CalculateDelay(int? minDelay, int? maxDelay)
    {
        if (!minDelay.HasValue && !maxDelay.HasValue)
            return 0;

        var min = minDelay ?? 0;
        var max = maxDelay ?? min;

        if (max < min)
            max = min;

        if (min == max)
            return min;

        var random = new Random();
        return random.Next(min, max + 1);
    }

    /// <summary>
    /// Creates a 404 Not Found HTTP response
    /// https://tools.ietf.org/html/rfc7231#section-6.5.4
    /// </summary>
    private static Task CreateNotFoundResponse(HttpContext context)
    {
        context.Response.ContentType = GetContentType(context) ?? TanukiConstants.DefaultErrorContentType;
        context.Response.StatusCode = 404;
        return context.Response.WriteAsync(string.Empty);
    }

    /// <summary>
    /// Creates a 405 Method Not Allowed HTTP response
    /// https://tools.ietf.org/html/rfc7231#section-6.5.5
    /// </summary>
    private static Task CreateMethodNotAllowedResponse(HttpContext context)
    {
        context.Response.ContentType = GetContentType(context) ?? TanukiConstants.DefaultErrorContentType;
        context.Response.StatusCode = 405;
        return context.Response.WriteAsync(string.Empty);
    }

    private static string? GetContentType(HttpContext context)
    {
        // Try to get content type from Accept header
        var acceptHeader = context.Request.Headers.ContainsKey("Accept") 
            ? context.Request.Headers["Accept"].ToString() 
            : string.Empty;
        if (!string.IsNullOrWhiteSpace(acceptHeader))
        {
            var parts = acceptHeader.Split(',');
            if (parts.Length > 0)
            {
                var firstPart = parts[0].Trim();
                var semicolonIndex = firstPart.IndexOf(';');
                return semicolonIndex >= 0 
                    ? firstPart[..semicolonIndex].Trim() 
                    : firstPart.Trim();
            }
        }

        // Fallback to request content type
        return context.Request.ContentType;
    }
}
