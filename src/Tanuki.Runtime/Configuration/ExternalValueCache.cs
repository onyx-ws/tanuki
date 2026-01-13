using Microsoft.Extensions.Caching.Memory;
using Onyx.Tanuki.Constants;

namespace Onyx.Tanuki.Configuration;

/// <summary>
/// In-memory cache for external values with configurable expiration
/// </summary>
public class ExternalValueCache : IExternalValueCache
{
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalValueCache"/> class
    /// </summary>
    /// <param name="memoryCache">The memory cache instance</param>
    public ExternalValueCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    /// <summary>
    /// Gets a cached external value, or null if not cached
    /// </summary>
    /// <param name="url">The URL of the external value</param>
    /// <returns>The cached value, or null if not found</returns>
    public string? Get(string url)
    {
        return _memoryCache.Get<string>(GetCacheKey(url));
    }

    /// <summary>
    /// Sets a cached external value with default expiration (60 minutes absolute, 30 minutes sliding)
    /// </summary>
    /// <param name="url">The URL of the external value</param>
    /// <param name="value">The value to cache</param>
    public void Set(string url, string value)
    {
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(TanukiConstants.DefaultCacheExpirationMinutes),
            SlidingExpiration = TimeSpan.FromMinutes(TanukiConstants.DefaultCacheSlidingExpirationMinutes),
            Priority = CacheItemPriority.Normal
        };

        _memoryCache.Set(GetCacheKey(url), value, cacheOptions);
    }

    private static string GetCacheKey(string url) => $"{TanukiConstants.CacheKeyPrefix}{url}";
}
