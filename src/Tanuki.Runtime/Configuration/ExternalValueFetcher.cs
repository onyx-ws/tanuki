using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Onyx.Tanuki.Configuration;

/// <summary>
/// Service for fetching external values from URLs
/// </summary>
public class ExternalValueFetcher : IExternalValueFetcher
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IExternalValueCache? _cache;
    private readonly ILogger<ExternalValueFetcher>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalValueFetcher"/> class
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory</param>
    /// <param name="cache">Optional cache for external values</param>
    /// <param name="logger">Optional logger instance</param>
    public ExternalValueFetcher(
        IHttpClientFactory httpClientFactory,
        IExternalValueCache? cache = null,
        ILogger<ExternalValueFetcher>? logger = null)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Fetches a value from an external URL
    /// </summary>
    /// <param name="url">The URL to fetch from</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The fetched value, or null if fetching failed</returns>
    /// <exception cref="ArgumentException">Thrown when the URL is invalid or uses an unsupported scheme</exception>
    public async Task<string?> FetchAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        // Validate URL format and scheme
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"Invalid URL format: '{url}'. URL must be a valid absolute URI.", nameof(url));
        }

        // Only allow HTTP and HTTPS schemes for security
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException(
                $"Unsupported URL scheme: '{uri.Scheme}'. Only HTTP and HTTPS URLs are allowed. URL: '{url}'",
                nameof(url));
        }

        // Check cache first
        if (_cache != null)
        {
            var cachedValue = _cache.Get(url);
            if (cachedValue != null)
            {
                _logger?.LogDebug("External value retrieved from cache for {Url}", url);
                return cachedValue;
            }
        }

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var value = await httpClient.GetStringAsync(url, cancellationToken);

            // Cache the value if cache is available
            if (_cache != null && value != null)
            {
                _cache.Set(url, value);
            }

            return value;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "Failed to fetch external value from {Url}", url);
            throw;
        }
        catch (TaskCanceledException)
        {
            _logger?.LogWarning("Fetching external value from {Url} was cancelled", url);
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error while fetching external value from {Url}", url);
            return null;
        }
    }
}
