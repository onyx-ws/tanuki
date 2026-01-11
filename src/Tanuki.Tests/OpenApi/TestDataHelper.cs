namespace Onyx.Tanuki.Tests.OpenApi;

/// <summary>
/// Helper class for accessing OpenAPI test data files.
/// </summary>
public static class TestDataHelper
{
    /// <summary>
    /// Gets the path to the test data directory.
    /// </summary>
    public static string TestDataDirectory => Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", // Go up from bin/Debug/net9.0 to project root
        "src", "Tanuki.Tests", "TestData", "OpenApi");

    /// <summary>
    /// Gets the full path to a test data file.
    /// </summary>
    public static string GetTestDataPath(string fileName)
    {
        return Path.Combine(TestDataDirectory, fileName);
    }

    /// <summary>
    /// Gets the Petstore YAML file path (OpenAPI 3.0).
    /// </summary>
    public static string PetstoreYaml => GetTestDataPath("petstore.yaml");

    /// <summary>
    /// Gets the Petstore JSON file path (OpenAPI 3.0).
    /// </summary>
    public static string PetstoreJson => GetTestDataPath("openapi.json");

    /// <summary>
    /// Gets the Petstore YAML file path from swagger.io (OpenAPI 3.0).
    /// </summary>
    public static string PetstoreSwaggerYaml => GetTestDataPath("openapi.yaml");
}
