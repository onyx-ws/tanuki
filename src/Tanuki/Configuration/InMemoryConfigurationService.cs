using Microsoft.Extensions.Logging;
using Onyx.Tanuki.Configuration.Exceptions;

namespace Onyx.Tanuki.Configuration;

/// <summary>
/// In-memory implementation of <see cref="ITanukiConfigurationService"/> that uses a pre-loaded Tanuki configuration
/// without file I/O. This is useful for CLI scenarios where the configuration is generated from OpenAPI specs.
/// </summary>
public class InMemoryConfigurationService : ITanukiConfigurationService
{
    private readonly IConfigurationValidator _configurationValidator;
    private readonly IExternalValueFetcher _externalValueFetcher;
    private readonly ILogger<InMemoryConfigurationService>? _logger;
    private bool _externalValuesFetched;
    private Dictionary<string, Path> _pathCache;
    private readonly object _reloadLock = new();

    /// <inheritdoc />
    public Tanuki Tanuki { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryConfigurationService"/> class.
    /// </summary>
    /// <param name="tanuki">The Tanuki configuration to use (must not be null).</param>
    /// <param name="configurationValidator">Validator for validating the configuration.</param>
    /// <param name="externalValueFetcher">Fetcher for external values.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentNullException">If tanuki, configurationValidator, or externalValueFetcher is null.</exception>
    public InMemoryConfigurationService(
        Tanuki tanuki,
        IConfigurationValidator configurationValidator,
        IExternalValueFetcher externalValueFetcher,
        ILogger<InMemoryConfigurationService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(tanuki);
        ArgumentNullException.ThrowIfNull(configurationValidator);
        ArgumentNullException.ThrowIfNull(externalValueFetcher);

        _configurationValidator = configurationValidator;
        _externalValueFetcher = externalValueFetcher;
        _logger = logger;

        // Validate the configuration
        try
        {
            _configurationValidator.Validate(tanuki);
        }
        catch (TanukiConfigurationException ex)
        {
            _logger?.LogError(ex, "Invalid Tanuki configuration provided to InMemoryConfigurationService");
            throw;
        }

        Tanuki = tanuki;
        _pathCache = BuildPathCache(Tanuki);
    }

    /// <summary>
    /// Updates the configuration in memory.
    /// </summary>
    /// <param name="tanuki">The new Tanuki configuration to use.</param>
    /// <exception cref="ArgumentNullException">If tanuki is null.</exception>
    /// <exception cref="TanukiConfigurationException">If the configuration is invalid.</exception>
    public void UpdateConfiguration(Tanuki tanuki)
    {
        ArgumentNullException.ThrowIfNull(tanuki);

        lock (_reloadLock)
        {
            // Validate the new configuration
            try
            {
                _configurationValidator.Validate(tanuki);
            }
            catch (TanukiConfigurationException ex)
            {
                _logger?.LogError(ex, "Invalid Tanuki configuration provided to UpdateConfiguration");
                throw;
            }

            Tanuki = tanuki;
            _pathCache = BuildPathCache(Tanuki);
            _externalValuesFetched = false; // Reset flag to allow re-fetching external values
            _logger?.LogInformation("Configuration updated successfully");
        }
    }

    /// <inheritdoc />
    public Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        // For in-memory service, reload is a no-op since there's no file to reload from
        // But we can reset the external values flag to allow re-fetching
        lock (_reloadLock)
        {
            _externalValuesFetched = false;
            _logger?.LogDebug("Reload requested (no-op for in-memory service, external values flag reset)");
        }
        return Task.CompletedTask;
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

    /// <inheritdoc />
    public Path? GetPathByUri(string uri)
    {
        lock (_reloadLock)
        {
            _pathCache.TryGetValue(uri, out var path);
            return path;
        }
    }

    /// <inheritdoc />
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
