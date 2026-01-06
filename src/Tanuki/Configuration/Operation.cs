namespace Onyx.Tanuki.Configuration;

/// <summary>
/// Describes a single API operation on a path.
/// </summary>
public class Operation
{
    /// <summary>
    /// The web operation name; defined as the HTTP action: GET; POST, etc...
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A short summary of what the operation does
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// A list of tags for API documentation control. Tags can be used for logical grouping of operations by resources or any other qualifier
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// A verbose explanation of the operation behavior. CommonMark syntax MAY be used for rich text representation
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Unique string used to identify the operation
    /// </summary>
    public string OperationId { get; set; } = string.Empty;

    /// <summary>
    /// The list of possible responses as they are returned from executing this operation
    /// </summary>
    public List<Response> Responses { get; set; } = [];

    /// <summary>
    /// Minimum delay in milliseconds before returning the response (for simulation purposes)
    /// </summary>
    public int? MinDelay { get; set; }

    /// <summary>
    /// Maximum delay in milliseconds before returning the response (for simulation purposes)
    /// </summary>
    public int? MaxDelay { get; set; }
}