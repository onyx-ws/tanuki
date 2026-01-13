namespace Onyx.Tanuki.Configuration;

/// <summary>
/// Service for loading Tanuki configuration from file
/// </summary>
public interface IConfigurationLoader
{
    /// <summary>
    /// Loads configuration from the specified file path
    /// </summary>
    /// <param name="filePath">Path to the configuration file</param>
    /// <returns>The loaded Tanuki configuration</returns>
    /// <exception cref="TanukiConfigurationException">Thrown when configuration cannot be loaded</exception>
    Tanuki LoadFromFile(string filePath);
}
