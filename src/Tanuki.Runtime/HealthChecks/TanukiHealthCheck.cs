using Microsoft.Extensions.Diagnostics.HealthChecks;
using Onyx.Tanuki.Configuration;

namespace Onyx.Tanuki.HealthChecks;

/// <summary>
/// Health check for Tanuki configuration that validates the loaded configuration
/// </summary>
public class TanukiHealthCheck : IHealthCheck
{
    private readonly ITanukiConfigurationService _configurationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TanukiHealthCheck"/> class
    /// </summary>
    /// <param name="configurationService">The Tanuki configuration service</param>
    public TanukiHealthCheck(ITanukiConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    /// <summary>
    /// Checks the health of the Tanuki configuration
    /// </summary>
    /// <param name="context">The health check context</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A task that represents the health check result</returns>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tanuki = _configurationService.Tanuki;

            if (tanuki.Paths.Count == 0)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Tanuki configuration has no paths defined."));
            }

            var totalOperations = tanuki.Paths.Sum(p => p.Operations.Count);
            if (totalOperations == 0)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Tanuki configuration has no operations defined."));
            }

            var totalResponses = tanuki.Paths
                .SelectMany(p => p.Operations)
                .Sum(o => o.Responses.Count);
            
            if (totalResponses == 0)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Tanuki configuration has no responses defined."));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Tanuki is configured with {tanuki.Paths.Count} path(s), {totalOperations} operation(s), and {totalResponses} response(s)."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Tanuki configuration is invalid or cannot be accessed.",
                ex));
        }
    }
}
