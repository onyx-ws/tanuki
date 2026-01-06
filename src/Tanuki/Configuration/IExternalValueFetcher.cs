namespace Onyx.Tanuki.Configuration;

/// <summary>
/// Service for fetching external values from URLs
/// </summary>
public interface IExternalValueFetcher
{
    /// <summary>
    /// Fetches a value from an external URL
    /// </summary>
    /// <param name="url">The URL to fetch from</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The fetched value, or null if fetching failed</returns>
    /// <exception cref="ArgumentException">Thrown when the URL is invalid or uses an unsupported scheme</exception>
    Task<string?> FetchAsync(string url, CancellationToken cancellationToken = default);
}
