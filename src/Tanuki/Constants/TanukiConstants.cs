namespace Onyx.Tanuki.Constants;

/// <summary>
/// Constants used throughout the Tanuki application
/// </summary>
public static class TanukiConstants
{
    /// <summary>
    /// Health check endpoint path
    /// </summary>
    public const string HealthCheckPath = "/health";

    /// <summary>
    /// Query parameter name for specifying status code
    /// </summary>
    public const string StatusQueryParameter = "status";

    /// <summary>
    /// Query parameter name for specifying example name
    /// </summary>
    public const string ExampleQueryParameter = "example";

    /// <summary>
    /// Query parameter name for random example selection
    /// </summary>
    public const string RandomQueryParameter = "random";

    /// <summary>
    /// Alternative query parameter name for random example selection
    /// </summary>
    public const string RandomQueryParameterAlt = "rand";

    /// <summary>
    /// Minimum valid HTTP status code
    /// </summary>
    public const int MinHttpStatusCode = 100;

    /// <summary>
    /// Maximum valid HTTP status code (exclusive)
    /// </summary>
    public const int MaxHttpStatusCode = 600;

    /// <summary>
    /// Default HTTP status code when invalid status code is encountered
    /// </summary>
    public const int DefaultHttpStatusCode = 200;

    /// <summary>
    /// Default cache expiration time in minutes
    /// </summary>
    public const int DefaultCacheExpirationMinutes = 60;

    /// <summary>
    /// Default cache sliding expiration time in minutes
    /// </summary>
    public const int DefaultCacheSlidingExpirationMinutes = 30;

    /// <summary>
    /// Cache key prefix for external values
    /// </summary>
    public const string CacheKeyPrefix = "tanuki:external:";

    /// <summary>
    /// Default content type for error responses
    /// </summary>
    public const string DefaultErrorContentType = "application/json";
}
