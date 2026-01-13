using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Onyx.Tanuki.Configuration;

/// <summary>
/// Service for loading and managing Tanuki configuration
/// </summary>
public class TanukiConfigurationService : ITanukiConfigurationService
{
    private readonly IOptionsMonitor<TanukiOptions> _optionsMonitor;
    private readonly IConfigurationLoader _configurationLoader;
    private readonly IConfigurationValidator _configurationValidator;
    private readonly IExternalValueFetcher _externalValueFetcher;
    private readonly ILogger<TanukiConfigurationService>? _logger;
    private bool _externalValuesFetched;
    private Dictionary<string, Path> _pathCache;
    private readonly object _reloadLock = new();

    public Tanuki Tanuki { get; private set; }

    public TanukiConfigurationService(
        IOptionsMonitor<TanukiOptions> optionsMonitor,
        IConfigurationLoader configurationLoader,
        IConfigurationValidator configurationValidator,
        IExternalValueFetcher externalValueFetcher,
        ILogger<TanukiConfigurationService>? logger = null)
    {
        _optionsMonitor = optionsMonitor;
        _configurationLoader = configurationLoader;
        _configurationValidator = configurationValidator;
        _externalValueFetcher = externalValueFetcher;
        _logger = logger;

        var options = _optionsMonitor.CurrentValue;
        Tanuki = LoadConfiguration(options.ConfigurationFilePath);
        _pathCache = BuildPathCache(Tanuki);

        // Subscribe to configuration changes
        _optionsMonitor.OnChange(OnConfigurationChanged);
    }

    private void OnConfigurationChanged(TanukiOptions options, string? name)
    {
        _logger?.LogInformation("Configuration options changed, reloading configuration...");
        try
        {
            ReloadAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to reload configuration after options change");
        }
    }

    private Tanuki LoadConfiguration(string configPath)
    {
        var tanuki = _configurationLoader.LoadFromFile(configPath);
        _configurationValidator.Validate(tanuki);
        return tanuki;
    }

    /// <summary>
    /// Reloads the configuration from file
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Task representing the reload operation</returns>
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        lock (_reloadLock)
        {
            var options = _optionsMonitor.CurrentValue;
            var newTanuki = LoadConfiguration(options.ConfigurationFilePath);
            
            Tanuki = newTanuki;
            _pathCache = BuildPathCache(newTanuki);
            _externalValuesFetched = false; // Reset flag to allow re-fetching external values
            
            _logger?.LogInformation("Configuration reloaded successfully");
        }

        // Re-fetch external values after reload
        await FetchExternalValuesAsync(cancellationToken);
    }

    private static Dictionary<string, Path> BuildPathCache(Tanuki tanuki)
    {
        var cache = new Dictionary<string, Path>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in tanuki.Paths)
        {
            if (!string.IsNullOrWhiteSpace(path.Uri))
            {
                cache[path.Uri] = path;
            }
        }
        return cache;
    }

    /// <summary>
    /// Gets a path by URI using optimized dictionary lookup
    /// </summary>
    /// <param name="uri">The path URI to look up</param>
    /// <returns>The matching Path, or null if not found</returns>
    public Path? GetPathByUri(string uri)
    {
        lock (_reloadLock)
        {
            _pathCache.TryGetValue(uri, out var path);
            return path;
        }
    }


    public async Task FetchExternalValuesAsync(CancellationToken cancellationToken = default)
    {
        // Thread-safe check and set flag to prevent concurrent execution
        lock (_reloadLock)
        {
            if (_externalValuesFetched)
                return;
            _externalValuesFetched = true; // Set early to prevent concurrent execution
        }

        try
        {
            // Flatten nested structure using LINQ SelectMany for better readability and performance
            var examplesToFetch = Tanuki.Paths
                .SelectMany(p => p.Operations)
                .SelectMany(o => o.Responses)
                .SelectMany(r => r.Content)
                .SelectMany(c => c.Examples)
                .Where(e => !string.IsNullOrWhiteSpace(e.ExternalValue) && 
                           string.IsNullOrWhiteSpace(e.Value))
                .ToList();

            if (examplesToFetch.Count == 0)
            {
                _logger?.LogDebug("No external values to fetch");
                return;
            }

            _logger?.LogInformation("Fetching {Count} external values in parallel", examplesToFetch.Count);

            // Process in parallel with concurrency limit to prevent overwhelming external servers
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = 10 // Limit to 10 concurrent requests
            };

            var fetchStartTime = DateTime.UtcNow;
            await Parallel.ForEachAsync(
                examplesToFetch,
                parallelOptions,
                async (example, ct) =>
                {
                    try
                    {
                        var startTime = DateTime.UtcNow;
                        var fetchedValue = await _externalValueFetcher.FetchAsync(
                            example.ExternalValue!, 
                            ct);
                        var duration = DateTime.UtcNow - startTime;

                        if (fetchedValue != null)
                        {
                            example.Value = fetchedValue;
                            _logger?.LogDebug(
                                "External value from {Url} loaded in {Duration}ms",
                                example.ExternalValue,
                                duration.TotalMilliseconds);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex,
                            "Failed to fetch external value from {Url}. The example will use the external URL as fallback.",
                            example.ExternalValue);
                    }
                });

            var totalDuration = DateTime.UtcNow - fetchStartTime;
            _logger?.LogInformation(
                "External values fetched successfully. Total: {Count} values in {Duration}ms",
                examplesToFetch.Count,
                totalDuration.TotalMilliseconds);
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("External value fetching was cancelled");
            lock (_reloadLock)
            {
                _externalValuesFetched = false; // Reset flag on cancellation
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error occurred while fetching external values");
            lock (_reloadLock)
            {
                _externalValuesFetched = false; // Reset flag on error to allow retry
            }
            throw;
        }
    }
}
