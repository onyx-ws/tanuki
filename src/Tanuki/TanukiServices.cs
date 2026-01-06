using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Onyx.Tanuki.Configuration;
using Onyx.Tanuki.HealthChecks;
using Onyx.Tanuki.Hosting;
using Onyx.Tanuki.Simulation;

namespace Onyx.Tanuki;

/// <summary>
/// Extension methods for adding Tanuki services
/// </summary>
public static class TanukiServices
{
    /// <summary>
    /// Adds Tanuki services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance (optional, will use builder.Configuration if not provided)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTanuki(
        this IServiceCollection services, 
        IConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Add HttpClientFactory if not already registered
        services.AddHttpClient();

        // Add memory cache for external value caching
        services.AddMemoryCache();
        services.TryAddSingleton<IExternalValueCache, ExternalValueCache>();

        // Configure TanukiOptions from configuration
        if (configuration != null)
        {
            services.Configure<TanukiOptions>(configuration.GetSection(TanukiOptions.SectionName));
        }
        else
        {
            // Use default configuration
            services.Configure<TanukiOptions>(options =>
            {
                options.ConfigurationFilePath = "./tanuki.json";
            });
        }

        // Register external value fetcher
        services.TryAddSingleton<IExternalValueFetcher, ExternalValueFetcher>();

        // Register configuration loader and validator
        services.TryAddSingleton<IConfigurationLoader, ConfigurationLoader>();
        services.TryAddSingleton<IConfigurationValidator, ConfigurationValidator>();

        // Register the configuration service as singleton (configuration is loaded once)
        services.TryAddSingleton<ITanukiConfigurationService, TanukiConfigurationService>();

        // Register ResponseSelector as scoped (stateless but can be per-request if needed)
        // Using singleton since it's stateless and thread-safe
        services.TryAddSingleton<IResponseSelector, ResponseSelector>();

        // Add health checks
        services.AddHealthChecks()
            .AddCheck<TanukiHealthCheck>("tanuki", tags: new[] { "tanuki", "configuration" });

        // Register hosted service for startup tasks (fetching external values)
        services.AddHostedService<TanukiStartupService>();

        return services;
    }
}
