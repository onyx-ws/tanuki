using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Onyx.Tanuki.Configuration.Exceptions;
using System.Net;
using System.Text.Json;

namespace Onyx.Tanuki.Middleware;

/// <summary>
/// Middleware that handles exceptions and returns consistent error responses
/// </summary>
public class TanukiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TanukiExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TanukiExceptionHandlingMiddleware"/> class
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    /// <param name="logger">The logger instance</param>
    public TanukiExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<TanukiExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to process the HTTP request
    /// </summary>
    /// <param name="context">The HTTP context</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (TanukiConfigurationException ex)
        {
            _logger.LogError(ex, "Tanuki configuration error occurred");
            await HandleConfigurationExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while processing the request");
            await HandleGenericExceptionAsync(context, ex);
        }
    }

    private static async Task HandleConfigurationExceptionAsync(HttpContext context, TanukiConfigurationException ex)
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            error = new
            {
                type = "ConfigurationError",
                message = ex.Message,
                statusCode = context.Response.StatusCode
            }
        };

        var json = JsonSerializer.Serialize(errorResponse);
        await context.Response.WriteAsync(json);
    }

    private static async Task HandleGenericExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            error = new
            {
                type = "InternalServerError",
                message = "An error occurred while processing your request.",
                statusCode = context.Response.StatusCode
            }
        };

        var json = JsonSerializer.Serialize(errorResponse);
        await context.Response.WriteAsync(json);
    }
}
