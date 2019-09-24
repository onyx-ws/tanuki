using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Onyx.Tanuki.Configuration.Json
{
    public class JsonResponseParser
    {
        public static Response Parse(JsonProperty json)
        {
            Response response = new Response()
            {
                StatusCode = json.Name
            };

            foreach (JsonProperty property in json.Value.EnumerateObject())
            {
                if (property.Name == "description") response.Description = property.Value.GetString();
                if (property.Name == "content") response.Content = ParseContent(property.Value);
            }

            return response;
        }

        private static List<Content> ParseContent(JsonElement json)
        {
            return json.EnumerateObject().Select(content => JsonContentParser.Parse(content)).ToList<Content>();
        }
    }
}
