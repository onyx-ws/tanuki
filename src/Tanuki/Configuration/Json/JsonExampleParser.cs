using System.Text.Json;
using Onyx.Tanuki.Configuration;
using Onyx.Tanuki.Configuration.Exceptions;

namespace Onyx.Tanuki.Configuration.Json;

public class JsonExampleParser
{
    public static Example Parse(JsonProperty json)
    {
        if (string.IsNullOrWhiteSpace(json.Name))
        {
            throw new TanukiConfigurationException(
                "Example name cannot be empty.");
        }

        if (json.Value.ValueKind != JsonValueKind.Object)
        {
            throw new TanukiConfigurationException(
                $"Example '{json.Name}' must be a JSON object. Found: {json.Value.ValueKind}");
        }

        try
        {
            var example = new Example
            {
                Name = json.Name
            };

            bool hasValue = false;
            bool hasExternalValue = false;

            foreach (var property in json.Value.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "summary":
                        if (property.Value.ValueKind == JsonValueKind.String)
                            example.Summary = property.Value.GetString() ?? string.Empty;
                        break;
                    
                    case "value":
                        hasValue = true;
                        example.Value = property.Value.ValueKind == JsonValueKind.String 
                            ? property.Value.GetString() 
                            : property.Value.ToString();
                        break;
                    
                    case "externalValue":
                        hasExternalValue = true;
                        if (property.Value.ValueKind == JsonValueKind.String)
                        {
                            var externalValue = property.Value.GetString();
                            if (string.IsNullOrWhiteSpace(externalValue))
                            {
                                throw new TanukiConfigurationException(
                                    $"The 'externalValue' property for example '{json.Name}' cannot be empty.");
                            }
                            example.ExternalValue = externalValue;
                        }
                        else
                        {
                            throw new TanukiConfigurationException(
                                $"The 'externalValue' property for example '{json.Name}' must be a string (URL).");
                        }
                        break;
                }
            }

            if (!hasValue && !hasExternalValue)
            {
                throw new TanukiConfigurationException(
                    $"Example '{json.Name}' must have either a 'value' or 'externalValue' property.");
            }

            if (hasValue && hasExternalValue)
            {
                throw new TanukiConfigurationException(
                    $"Example '{json.Name}' cannot have both 'value' and 'externalValue' properties. They are mutually exclusive.");
            }

            return example;
        }
        catch (TanukiConfigurationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new TanukiConfigurationException(
                $"An error occurred while parsing example '{json.Name}'. Please check the example structure.", ex);
        }
    }
}
