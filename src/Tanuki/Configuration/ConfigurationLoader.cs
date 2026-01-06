using Microsoft.Extensions.Logging;
using Onyx.Tanuki.Configuration.Exceptions;
using Onyx.Tanuki.Configuration.Json;

namespace Onyx.Tanuki.Configuration;

/// <summary>
/// Service for loading Tanuki configuration from file
/// </summary>
public class ConfigurationLoader : IConfigurationLoader
{
    private readonly ILogger<ConfigurationLoader>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationLoader"/> class
    /// </summary>
    /// <param name="logger">Optional logger instance</param>
    public ConfigurationLoader(ILogger<ConfigurationLoader>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loads configuration from the specified file path
    /// </summary>
    /// <param name="filePath">Path to the configuration file</param>
    /// <returns>The loaded Tanuki configuration</returns>
    /// <exception cref="TanukiConfigurationException">Thrown when configuration cannot be loaded</exception>
    public Tanuki LoadFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new TanukiConfigurationException(
                "Configuration file path is not specified. Set Tanuki:ConfigurationFilePath in appsettings.json or use the default './tanuki.json'");
        }

        if (!File.Exists(filePath))
        {
            throw new TanukiConfigurationException(
                $"Configuration file not found: {filePath}. Please ensure the file exists or update Tanuki:ConfigurationFilePath in appsettings.json");
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var tanuki = JsonConfigParser.Parse(json);

            _logger?.LogInformation("Tanuki configuration loaded successfully from {ConfigPath}", filePath);
            return tanuki;
        }
        catch (TanukiConfigurationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new TanukiConfigurationException(
                $"Failed to parse configuration file '{filePath}'. Please check the file format.", ex);
        }
    }
}
