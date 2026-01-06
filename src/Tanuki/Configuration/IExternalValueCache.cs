namespace Onyx.Tanuki.Configuration;

/// <summary>
/// Cache for external values to avoid repeated HTTP fetches
/// </summary>
public interface IExternalValueCache
{
    /// <summary>
    /// Gets a cached external value, or null if not cached
    /// </summary>
    /// <param name="url">The URL of the external value</param>
    /// <returns>The cached value, or null if not found</returns>
    string? Get(string url);

    /// <summary>
    /// Sets a cached external value
    /// </summary>
    /// <param name="url">The URL of the external value</param>
    /// <param name="value">The value to cache</param>
    void Set(string url, string value);
}
