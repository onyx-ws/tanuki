namespace Onyx.Tanuki.Configuration;

/// <summary>
/// Service for validating Tanuki configuration structure
/// </summary>
public interface IConfigurationValidator
{
    /// <summary>
    /// Validates the Tanuki configuration structure
    /// </summary>
    /// <param name="tanuki">The configuration to validate</param>
    /// <exception cref="TanukiConfigurationException">Thrown when configuration is invalid</exception>
    void Validate(Tanuki tanuki);
}
