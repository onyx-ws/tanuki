using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Onyx.Tanuki.Configuration.Json
{
    public class JsonOperationParser
    {
        public static Operation Parse(JsonProperty json)
        {
            Operation operation = new Operation();
            operation.Name = json.Name;

            foreach (JsonProperty property in json.Value.EnumerateObject())
            {
                if (property.Name == "tags") operation.Tags = ParseTags(property.Value);
                if (property.Name == "summary") operation.Summary = property.Value.GetString();
                if (property.Name == "description") operation.Description = property.Value.GetString();
                if (property.Name == "operationId") operation.OperationId = property.Value.GetString();
                if (property.Name == "responses") operation.Responses = ParseResponses(property.Value);
            }
            return operation;
        }

        private static List<string> ParseTags(JsonElement json)
        {
            return json.EnumerateArray().Select(s => s.GetString()).ToList<string>();
        }

        private static List<Response> ParseResponses(JsonElement json)
        {
            return json.EnumerateObject().Select(response => JsonResponseParser.Parse(response)).ToList<Response>();
        }
    }
}
