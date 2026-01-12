namespace Onyx.Tanuki.Tests.OpenApi;

/// <summary>
/// Helper class for accessing OpenAPI test data files.
/// </summary>
public static class TestDataHelper
{
    /// <summary>
    /// Gets the path to the test data directory.
    /// </summary>
    public static string TestDataDirectory
    {
        get
        {
            // Start from the current directory and search upward for the project root
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            
            // Search for the solution root by looking for the .sln file or src directory
            while (directory != null && directory.Parent != null)
            {
                // Check if we're at the repository root (contains src directory)
                var srcDir = Path.Combine(directory.FullName, "src");
                if (Directory.Exists(srcDir))
                {
                    var testDataPath = Path.Combine(srcDir, "Tanuki.Tests", "TestData", "OpenApi");
                    if (Directory.Exists(testDataPath))
                    {
                        return testDataPath;
                    }
                }
                
                directory = directory.Parent;
            }
            
            // Fallback to the original approach if directory search fails
            return Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..", // Go up from bin/Debug/net9.0 to project root
                "src", "Tanuki.Tests", "TestData", "OpenApi");
        }
    }

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
