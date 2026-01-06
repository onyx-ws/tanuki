namespace Onyx.Tanuki.Configuration;

/// <summary>
/// Defines an example response payload, which can be an inline value or an external URL
/// </summary>
public class Example
{
    /// <summary>
    /// The name of the example, used for selection via query parameters
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Short description for the example.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Long description for the example. CommonMark syntax MAY be used for rich text representation.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// A URL that points to the literal example. This provides the capability to reference examples that cannot easily be included in JSON or YAML documents. The value field and externalValue field are mutually exclusive.
    /// </summary>
    public string? ExternalValue { get; set; }

    /// <summary>
    /// Embedded literal example. The value field and externalValue field are mutually exclusive. To represent examples of media types that cannot naturally represented in JSON or YAML, use a string value to contain the example, escaping where necessary.
    /// </summary>
    public string? Value { get; set; }

}