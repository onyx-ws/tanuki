namespace Onyx.Tanuki.Configuration;

/// <summary>
/// Service for loading and managing Tanuki configuration
/// </summary>
public interface ITanukiConfigurationService
{
    /// <summary>
    /// Gets the Tanuki configuration
    /// </summary>
    Tanuki Tanuki { get; }

    /// <summary>
    /// Gets a path by URI using optimized dictionary lookup
    /// </summary>
    /// <param name="uri">The path URI to look up</param>
    /// <returns>The matching Path, or null if not found</returns>
    Path? GetPathByUri(string uri);

    /// <summary>
    /// Reloads the configuration from file
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Task representing the reload operation</returns>
    Task ReloadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all external values asynchronously
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    Task FetchExternalValuesAsync(CancellationToken cancellationToken = default);
}
