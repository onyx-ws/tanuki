using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace Tanuki.Cli.Middleware;

/// <summary>
/// Middleware that logs HTTP requests and responses in a developer-friendly format
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private const int MaxHeaderValueLength = 100;
    private const int MaxBodyPreviewLength = 500;
    private const int MaxRequestBodySize = 1024 * 1024; // 1MB limit for request body logging

    // Sensitive headers that should be masked
    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-API-Key",
        "X-Auth-Token",
        "X-Access-Token"
    };

    private readonly RequestDelegate _next;
    private readonly bool _verbose;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        bool verbose = false)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _verbose = verbose;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestMethod = context.Request.Method;
        var requestPath = context.Request.Path;
        var queryString = context.Request.QueryString;
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Skip logging for health checks unless verbose
        var isHealthCheck = requestPath.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);

        if (isHealthCheck && !_verbose)
        {
            await _next(context);
            return;
        }

        // Capture request body (with size limit to prevent DoS)
        string? requestBody = null;
        var contentLength = context.Request.ContentLength ?? 0;
        if (contentLength > 0 && contentLength <= MaxRequestBodySize)
        {
            try
            {
                context.Request.EnableBuffering();
                var originalPosition = context.Request.Body.Position;
                context.Request.Body.Position = 0;
                
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = originalPosition;
            }
            catch (Exception ex)
            {
                // Log error but don't fail the request
                Console.WriteLine($"Warning: Failed to read request body: {ex.Message}");
            }
        }
        else if (contentLength > MaxRequestBodySize)
        {
            requestBody = $"[Request body too large ({contentLength} bytes), max size: {MaxRequestBodySize} bytes]";
        }

        // Capture response body
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Log request details
        LogRequest(context, requestMethod, requestPath, queryString, remoteIp, requestBody);

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log exception but don't mask it
            Console.WriteLine($"ERROR: Request processing failed: {ex.Message}");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var duration = stopwatch.ElapsedMilliseconds;

            // Capture response body
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(responseBodyStream, Encoding.UTF8).ReadToEndAsync();
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBodyStream);

            // Log response details
            LogResponse(context, duration, responseBody);
        }
    }

    private void LogRequest(HttpContext context, string method, PathString path, QueryString queryString, string remoteIp, string? body)
    {
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine($"REQUEST: {method} {path}{queryString}");
        Console.WriteLine($"From: {remoteIp}");
        
        // Log headers (mask sensitive headers)
        Console.WriteLine("Headers:");
        foreach (var header in context.Request.Headers)
        {
            var headerName = header.Key;
            var value = header.Value.ToString();
            
            // Mask sensitive headers
            if (SensitiveHeaders.Contains(headerName))
            {
                value = "[REDACTED]";
            }
            // Truncate very long header values
            else if (value.Length > MaxHeaderValueLength)
            {
                value = value.Substring(0, MaxHeaderValueLength) + "...";
            }
            
            Console.WriteLine($"  {headerName}: {value}");
        }

        // Log body if present
        if (!string.IsNullOrEmpty(body))
        {
            Console.WriteLine("Body:");
            var bodyPreview = body.Length > MaxBodyPreviewLength 
                ? body.Substring(0, MaxBodyPreviewLength) + "\n... (truncated)" 
                : body;
            Console.WriteLine(bodyPreview);
        }
        else if (context.Request.ContentLength > 0)
        {
            Console.WriteLine("Body: <binary or non-readable content>");
        }

        Console.WriteLine("───────────────────────────────────────────────────────────");
    }

    private void LogResponse(HttpContext context, long duration, string body)
    {
        var statusCode = context.Response.StatusCode;
        var contentType = context.Response.ContentType ?? "unknown";
        var statusColor = statusCode >= 200 && statusCode < 300 ? "✓" : statusCode >= 400 ? "✗" : "→";

        Console.WriteLine($"RESPONSE: {statusColor} {statusCode} {contentType} | Duration: {duration}ms");
        
        // Log headers (mask sensitive headers)
        Console.WriteLine("Headers:");
        foreach (var header in context.Response.Headers)
        {
            var headerName = header.Key;
            var value = header.Value.ToString();
            
            // Mask sensitive headers
            if (SensitiveHeaders.Contains(headerName))
            {
                value = "[REDACTED]";
            }
            // Truncate very long header values
            else if (value.Length > MaxHeaderValueLength)
            {
                value = value.Substring(0, MaxHeaderValueLength) + "...";
            }
            
            Console.WriteLine($"  {headerName}: {value}");
        }

        // Log body
        if (!string.IsNullOrEmpty(body))
        {
            Console.WriteLine("Body:");
            var bodyPreview = body.Length > MaxBodyPreviewLength 
                ? body.Substring(0, MaxBodyPreviewLength) + "\n... (truncated)" 
                : body;
            Console.WriteLine(bodyPreview);
        }

        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine();
    }
}
