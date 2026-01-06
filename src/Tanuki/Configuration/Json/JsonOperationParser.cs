using System.Linq;
using System.Text.Json;
using Onyx.Tanuki.Configuration;
using Onyx.Tanuki.Configuration.Exceptions;

namespace Onyx.Tanuki.Configuration.Json;

public class JsonOperationParser
{
    public static Operation Parse(JsonProperty json)
    {
        if (json.Value.ValueKind != JsonValueKind.Object)
        {
            throw new TanukiConfigurationException(
                $"Operation '{json.Name}' must be a JSON object. Found: {json.Value.ValueKind}");
        }

        try
        {
            var operation = new Operation
            {
                Name = json.Name
            };

            foreach (var property in json.Value.EnumerateObject())
            {
                try
                {
                    switch (property.Name)
                    {
                        case "tags":
                            operation.Tags = ParseTags(property.Value, json.Name);
                            break;
                        
                        case "summary":
                            if (property.Value.ValueKind == JsonValueKind.String)
                                operation.Summary = property.Value.GetString() ?? string.Empty;
                            break;
                        
                        case "description":
                            if (property.Value.ValueKind == JsonValueKind.String)
                                operation.Description = property.Value.GetString() ?? string.Empty;
                            break;
                        
                        case "operationId":
                            if (property.Value.ValueKind == JsonValueKind.String)
                                operation.OperationId = property.Value.GetString() ?? string.Empty;
                            break;
                        
                        case "responses":
                            if (property.Value.ValueKind == JsonValueKind.Object)
                                operation.Responses = ParseResponses(property.Value, json.Name);
                            else
                                throw new TanukiConfigurationException(
                                    $"The 'responses' property for operation '{json.Name}' must be a JSON object.");
                            break;
                        
                        case "min-delay":
                            if (property.Value.ValueKind == JsonValueKind.Number)
                            {
                                operation.MinDelay = property.Value.GetInt32();
                                if (operation.MinDelay < 0)
                                    throw new TanukiConfigurationException(
                                        $"The 'min-delay' for operation '{json.Name}' must be a non-negative number.");
                            }
                            break;
                        
                        case "max-delay":
                            if (property.Value.ValueKind == JsonValueKind.Number)
                            {
                                operation.MaxDelay = property.Value.GetInt32();
                                if (operation.MaxDelay < 0)
                                    throw new TanukiConfigurationException(
                                        $"The 'max-delay' for operation '{json.Name}' must be a non-negative number.");
                                
                                if (operation.MinDelay.HasValue && operation.MaxDelay < operation.MinDelay)
                                    throw new TanukiConfigurationException(
                                        $"The 'max-delay' ({operation.MaxDelay}) for operation '{json.Name}' must be greater than or equal to 'min-delay' ({operation.MinDelay}).");
                            }
                            break;
                    }
                }
                catch (TanukiConfigurationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new TanukiConfigurationException(
                        $"Error parsing property '{property.Name}' for operation '{json.Name}': {ex.Message}", ex);
                }
            }

            // Validate required properties
            if (operation.Responses.Count == 0)
            {
                throw new TanukiConfigurationException(
                    $"Operation '{json.Name}' must have at least one response defined in the 'responses' property.");
            }

            return operation;
        }
        catch (TanukiConfigurationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new TanukiConfigurationException(
                $"An error occurred while parsing operation '{json.Name}'. Please check the operation structure.", ex);
        }
    }

    private static List<string> ParseTags(JsonElement json, string operationName)
    {
        if (json.ValueKind != JsonValueKind.Array)
        {
            throw new TanukiConfigurationException(
                $"The 'tags' property for operation '{operationName}' must be a JSON array.");
        }

        try
        {
            return json.EnumerateArray()
                .Select((s, index) =>
                {
                    if (s.ValueKind != JsonValueKind.String)
                    {
                        throw new TanukiConfigurationException(
                            $"Tag at index {index} for operation '{operationName}' must be a string.");
                    }
                    return s.GetString()!;
                })
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }
        catch (TanukiConfigurationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new TanukiConfigurationException(
                $"Error parsing tags for operation '{operationName}': {ex.Message}", ex);
        }
    }

    private static List<Response> ParseResponses(JsonElement json, string operationName)
    {
        try
        {
            var responses = json.EnumerateObject()
                .Select(response =>
                {
                    try
                    {
                        return JsonResponseParser.Parse(response);
                    }
                    catch (TanukiConfigurationException ex)
                    {
                        throw new TanukiConfigurationException(
                            $"Error parsing response '{response.Name}' for operation '{operationName}': {ex.Message}", ex);
                    }
                })
                .ToList();

            return responses;
        }
        catch (TanukiConfigurationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new TanukiConfigurationException(
                $"Error parsing responses for operation '{operationName}': {ex.Message}", ex);
        }
    }
}
