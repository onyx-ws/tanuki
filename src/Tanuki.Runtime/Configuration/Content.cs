namespace Onyx.Tanuki.Configuration;

/// <summary>
/// Describes the content of a response for a specific media type
/// </summary>
public class Content
{
    /// <summary>
    /// The media type definitions SHOULD be in compliance with RFC6838.
    /// Examples: "application/json", "application/xml", "text/plain"
    /// </summary>
    public string MediaType { get; set; } = string.Empty;

    /// <summary>
    /// Examples of the media type. Each example object SHOULD match the media type and specified schema if present. 
    /// The examples field is mutually exclusive of the example field. Furthermore, if referencing a schema which contains 
    /// an example, the examples value SHALL override the example provided by the schema.
    /// </summary>
    public List<Example> Examples { get; set; } = [];
}
