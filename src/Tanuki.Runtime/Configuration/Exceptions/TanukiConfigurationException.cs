namespace Onyx.Tanuki.Configuration.Exceptions;

/// <summary>
/// Exception thrown when Tanuki configuration is invalid or missing
/// </summary>
public class TanukiConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TanukiConfigurationException"/> class with a specified error message
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public TanukiConfigurationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TanukiConfigurationException"/> class with a specified error message and a reference to the inner exception
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public TanukiConfigurationException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
