using System.Linq;
using System.Text.Json;
using Onyx.Tanuki.Configuration;
using Onyx.Tanuki.Configuration.Exceptions;

namespace Onyx.Tanuki.Configuration.Json;

public class JsonContentParser
{
    public static Content Parse(JsonProperty json)
    {
        if (string.IsNullOrWhiteSpace(json.Name))
        {
            throw new TanukiConfigurationException(
                "Content media type cannot be empty.");
        }

        if (json.Value.ValueKind != JsonValueKind.Object)
        {
            throw new TanukiConfigurationException(
                $"Content '{json.Name}' must be a JSON object. Found: {json.Value.ValueKind}");
        }

        try
        {
            var content = new Content
            {
                MediaType = json.Name
            };

            foreach (var property in json.Value.EnumerateObject())
            {
                if (property.Name == "examples")
                {
                    if (property.Value.ValueKind == JsonValueKind.Object)
                        content.Examples = ParseExamples(property.Value, json.Name);
                    else
                        throw new TanukiConfigurationException(
                            $"The 'examples' property for content '{json.Name}' must be a JSON object.");
                }
            }

            if (content.Examples.Count == 0)
            {
                throw new TanukiConfigurationException(
                    $"Content '{json.Name}' must have at least one example defined in the 'examples' property.");
            }

            return content;
        }
        catch (TanukiConfigurationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new TanukiConfigurationException(
                $"An error occurred while parsing content '{json.Name}'. Please check the content structure.", ex);
        }
    }

    private static List<Example> ParseExamples(JsonElement json, string mediaType)
    {
        try
        {
            var examples = json.EnumerateObject()
                .Select(example =>
                {
                    try
                    {
                        return JsonExampleParser.Parse(example);
                    }
                    catch (TanukiConfigurationException ex)
                    {
                        throw new TanukiConfigurationException(
                            $"Error parsing example '{example.Name}' for content '{mediaType}': {ex.Message}", ex);
                    }
                })
                .ToList();

            return examples;
        }
        catch (TanukiConfigurationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new TanukiConfigurationException(
                $"Error parsing examples for content '{mediaType}': {ex.Message}", ex);
        }
    }
}
