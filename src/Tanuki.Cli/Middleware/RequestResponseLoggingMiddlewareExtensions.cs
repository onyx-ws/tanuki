using Microsoft.AspNetCore.Builder;

namespace Tanuki.Cli.Middleware;

/// <summary>
/// Extension methods for registering RequestResponseLoggingMiddleware
/// </summary>
public static class RequestResponseLoggingMiddlewareExtensions
{
    /// <summary>
    /// Adds the RequestResponseLoggingMiddleware to the application pipeline.
    /// This middleware logs HTTP requests and responses in a developer-friendly format.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="verbose">Whether to enable verbose logging (includes health checks)</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseRequestResponseLogging(
        this IApplicationBuilder app,
        bool verbose = false)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        return app.UseMiddleware<RequestResponseLoggingMiddleware>(verbose);
    }
}
