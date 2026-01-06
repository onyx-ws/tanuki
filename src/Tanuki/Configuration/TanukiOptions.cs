using Microsoft.Extensions.Configuration;

namespace Onyx.Tanuki.Configuration;

/// <summary>
/// Configuration options for Tanuki
/// </summary>
public class TanukiOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Tanuki";

    /// <summary>
    /// Path to the tanuki.json configuration file
    /// </summary>
    public string ConfigurationFilePath { get; set; } = "./tanuki.json";

    /// <summary>
    /// The Tanuki configuration data
    /// </summary>
    public Tanuki Tanuki { get; set; } = new();
}
