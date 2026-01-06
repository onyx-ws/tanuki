using System.Linq;
using System.Text.Json;
using Onyx.Tanuki.Configuration;
using Onyx.Tanuki.Configuration.Exceptions;

namespace Onyx.Tanuki.Configuration.Json;

public class JsonResponseParser
{
    public static Response Parse(JsonProperty json)
    {
        if (string.IsNullOrWhiteSpace(json.Name))
        {
            throw new TanukiConfigurationException(
                "Response status code cannot be empty.");
        }

        if (!int.TryParse(json.Name, out var statusCode) || statusCode < 100 || statusCode >= 600)
        {
            throw new TanukiConfigurationException(
                $"Invalid response status code '{json.Name}'. Status codes must be between 100 and 599.");
        }

        if (json.Value.ValueKind != JsonValueKind.Object)
        {
            throw new TanukiConfigurationException(
                $"Response '{json.Name}' must be a JSON object. Found: {json.Value.ValueKind}");
        }

        try
        {
            var response = new Response
            {
                StatusCode = json.Name
            };

            foreach (var property in json.Value.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "description":
                        if (property.Value.ValueKind == JsonValueKind.String)
                            response.Description = property.Value.GetString() ?? string.Empty;
                        break;
                    
                    case "content":
                        if (property.Value.ValueKind == JsonValueKind.Object)
                            response.Content = ParseContent(property.Value, json.Name);
                        else
                            throw new TanukiConfigurationException(
                                $"The 'content' property for response '{json.Name}' must be a JSON object.");
                        break;
                }
            }

            if (response.Content.Count == 0)
            {
                throw new TanukiConfigurationException(
                    $"Response '{json.Name}' must have at least one content type defined in the 'content' property.");
            }

            return response;
        }
        catch (TanukiConfigurationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new TanukiConfigurationException(
                $"An error occurred while parsing response '{json.Name}'. Please check the response structure.", ex);
        }
    }

    private static List<Content> ParseContent(JsonElement json, string statusCode)
    {
        try
        {
            var contents = json.EnumerateObject()
                .Select(content =>
                {
                    try
                    {
                        return JsonContentParser.Parse(content);
                    }
                    catch (TanukiConfigurationException ex)
                    {
                        throw new TanukiConfigurationException(
                            $"Error parsing content '{content.Name}' for response '{statusCode}': {ex.Message}", ex);
                    }
                })
                .ToList();

            return contents;
        }
        catch (TanukiConfigurationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new TanukiConfigurationException(
                $"Error parsing content for response '{statusCode}': {ex.Message}", ex);
        }
    }
}
