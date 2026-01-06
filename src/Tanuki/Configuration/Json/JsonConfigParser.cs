using System.Text.Json;
using Onyx.Tanuki.Configuration;
using Onyx.Tanuki.Configuration.Exceptions;

namespace Onyx.Tanuki.Configuration.Json;

public class JsonConfigParser
{
    public static Tanuki Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new TanukiConfigurationException(
                "Configuration JSON is null or empty. Please provide a valid JSON configuration.");
        }

        JsonDocument? jDocument = null;
        try
        {
            jDocument = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
        }
        catch (JsonException ex)
        {
            throw new TanukiConfigurationException(
                $"Invalid JSON format. Please check your configuration file syntax. Error: {ex.Message}", ex);
        }

        try
        {
            using (jDocument)
            {
                var jTanuki = jDocument.RootElement;

                if (jTanuki.ValueKind != JsonValueKind.Object)
                {
                    throw new TanukiConfigurationException(
                        "Configuration root must be a JSON object. Found: " + jTanuki.ValueKind);
                }

                var tanuki = new Tanuki
                {
                    Paths = JsonPathsParser.Parse(jTanuki)
                };

                return tanuki;
            }
        }
        catch (TanukiConfigurationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new TanukiConfigurationException(
                "An error occurred while parsing the configuration. Please check the configuration structure.", ex);
        }
    }
}
