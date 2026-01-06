using Microsoft.AspNetCore.Builder;
using Onyx.Tanuki.Middleware;
using Onyx.Tanuki.Simulation;

namespace Onyx.Tanuki;

/// <summary>
/// Extension methods for configuring Tanuki middleware in the application pipeline
/// </summary>
public static class TanukiBuilder
{
    /// <summary>
    /// Adds the Tanuki simulation middleware to the application pipeline.
    /// This is an alias for <see cref="UseSimulator"/>.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseTanuki(this IApplicationBuilder app)
    {
        return UseSimulator(app);
    }

    /// <summary>
    /// Adds the Tanuki exception handling middleware to the application pipeline.
    /// This middleware should be added early in the pipeline to catch exceptions from downstream middleware.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseTanukiExceptionHandling(this IApplicationBuilder app)
    {
        app.UseMiddleware<TanukiExceptionHandlingMiddleware>();
        return app;
    }

    /// <summary>
    /// Adds the Tanuki simulation middleware to the application pipeline.
    /// This middleware intercepts requests and returns mock responses based on the tanuki.json configuration.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseSimulator(this IApplicationBuilder app)
    {
        app.UseMiddleware<SimulationMiddleware>();
        return app;
    }
}
