using Onyx.Tanuki.Configuration.Exceptions;

namespace Onyx.Tanuki.Configuration;

/// <summary>
/// Service for validating Tanuki configuration structure
/// </summary>
public class ConfigurationValidator : IConfigurationValidator
{
    /// <summary>
    /// Validates the Tanuki configuration structure
    /// </summary>
    /// <param name="tanuki">The configuration to validate</param>
    /// <exception cref="TanukiConfigurationException">Thrown when configuration is invalid</exception>
    public void Validate(Tanuki tanuki)
    {
        if (tanuki.Paths.Count == 0)
        {
            throw new TanukiConfigurationException(
                "Configuration must contain at least one path. No paths were found in the configuration file.");
        }

        foreach (var path in tanuki.Paths)
        {
            if (string.IsNullOrWhiteSpace(path.Uri))
            {
                throw new TanukiConfigurationException(
                    "Invalid configuration: Path URI cannot be empty.");
            }

            if (path.Operations.Count == 0)
            {
                throw new TanukiConfigurationException(
                    $"Invalid configuration: Path '{path.Uri}' must have at least one operation.");
            }

            foreach (var operation in path.Operations)
            {
                if (string.IsNullOrWhiteSpace(operation.Name))
                {
                    throw new TanukiConfigurationException(
                        $"Invalid configuration: Operation name cannot be empty for path '{path.Uri}'.");
                }

                if (operation.Responses.Count == 0)
                {
                    throw new TanukiConfigurationException(
                        $"Invalid configuration: Operation '{operation.Name}' on path '{path.Uri}' must have at least one response.");
                }
            }
        }
    }
}
