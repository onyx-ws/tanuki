using System.Linq;
using System.Text.Json;
using Onyx.Tanuki.Configuration;
using Onyx.Tanuki.Configuration.Exceptions;

namespace Onyx.Tanuki.Configuration.Json;

public class JsonPathsParser
{
    public static List<Path> Parse(JsonElement tanuki)
    {
        if (!tanuki.TryGetProperty("paths", out var jPaths))
        {
            throw new TanukiConfigurationException(
                "Configuration must contain a 'paths' property. No 'paths' property found in the root object.");
        }

        if (jPaths.ValueKind != JsonValueKind.Object)
        {
            throw new TanukiConfigurationException(
                $"The 'paths' property must be a JSON object. Found: {jPaths.ValueKind}");
        }

        try
        {
            var paths = jPaths.EnumerateObject()
                .Select(jPath => ParsePath(jPath))
                .ToList();

            if (paths.Count == 0)
            {
                throw new TanukiConfigurationException(
                    "The 'paths' object is empty. At least one path must be defined.");
            }

            return paths;
        }
        catch (TanukiConfigurationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new TanukiConfigurationException(
                "An error occurred while parsing paths. Please check the 'paths' structure.", ex);
        }
    }

    private static Path ParsePath(JsonProperty jPath)
    {
        if (string.IsNullOrWhiteSpace(jPath.Name))
        {
            throw new TanukiConfigurationException(
                "Path URI cannot be empty. All paths must have a non-empty URI.");
        }

        if (jPath.Value.ValueKind != JsonValueKind.Object)
        {
            throw new TanukiConfigurationException(
                $"Path '{jPath.Name}' must be a JSON object. Found: {jPath.Value.ValueKind}");
        }

        try
        {
            var path = new Path
            {
                Uri = jPath.Name,
                Operations = []
            };

            foreach (var property in jPath.Value.EnumerateObject())
            {
                var httpMethod = property.Name.ToLowerInvariant();
                if (httpMethod is "get" or "put" or "post" or "delete" or "options" or "head" or "patch" or "trace")
                {
                    try
                    {
                        path.Operations.Add(JsonOperationParser.Parse(property));
                    }
                    catch (TanukiConfigurationException ex)
                    {
                        throw new TanukiConfigurationException(
                            $"Error parsing {httpMethod.ToUpperInvariant()} operation for path '{jPath.Name}': {ex.Message}", ex);
                    }
                }
                else
                {
                    // Log or ignore unknown properties - they might be valid extensions
                    // For now, we'll silently ignore them
                }
            }

            if (path.Operations.Count == 0)
            {
                throw new TanukiConfigurationException(
                    $"Path '{jPath.Name}' must have at least one HTTP method operation (GET, POST, PUT, DELETE, etc.).");
            }

            return path;
        }
        catch (TanukiConfigurationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new TanukiConfigurationException(
                $"An error occurred while parsing path '{jPath.Name}'. Please check the path structure.", ex);
        }
    }
}