using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Onyx.Tanuki.Configuration;

namespace Onyx.Tanuki.Hosting;

/// <summary>
/// Hosted service that fetches external values during application startup
/// </summary>
public class TanukiStartupService : IHostedService
{
    private readonly ITanukiConfigurationService _configurationService;
    private readonly ILogger<TanukiStartupService> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="TanukiStartupService"/> class
    /// </summary>
    /// <param name="configurationService">The Tanuki configuration service</param>
    /// <param name="logger">The logger instance</param>
    /// <param name="environment">The host environment</param>
    public TanukiStartupService(
        ITanukiConfigurationService configurationService,
        ILogger<TanukiStartupService> logger,
        IHostEnvironment environment)
    {
        _configurationService = configurationService;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Starts the hosted service and fetches external values
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Skip in test environment where WebApplicationFactory handles initialization differently
        if (string.Equals(_environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Skipping external value fetch in Testing environment");
            return;
        }

        _logger.LogInformation("Fetching external values before accepting requests...");
        try
        {
            await _configurationService.FetchExternalValuesAsync(cancellationToken);
            _logger.LogInformation("External values fetched successfully. Server ready to accept requests.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("External value fetch was cancelled during startup");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, 
                "Failed to fetch external values during startup. Server will start anyway, but external values may not be available for initial requests.");
        }
    }

    /// <summary>
    /// Stops the hosted service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        // No cleanup needed
        return Task.CompletedTask;
    }
}
