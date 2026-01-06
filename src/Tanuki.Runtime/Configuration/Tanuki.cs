namespace Onyx.Tanuki.Configuration;

/// <summary>
/// Contains Tanuki configuration objects
/// </summary>
public class Tanuki
{
    /// <summary>
    /// The list of paths of API calls to be simulated
    /// </summary>
    public List<Path> Paths { get; set; } = [];
}
