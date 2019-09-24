using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Onyx.Tanuki.Configuration.Json
{
    public class JsonContentParser
    {
        public static Content Parse(JsonProperty json)
        {
            Content content = new Content
            {
                MediaType = json.Name
            };

            foreach (JsonProperty property in json.Value.EnumerateObject())
            {
                if (property.Name == "examples") content.Examples = ParseExamples(property.Value);
            }
            return content;
        }

        private static List<Example> ParseExamples(JsonElement json)
        {
            return json.EnumerateObject().Select(example => JsonExampleParser.Parse(example)).ToList<Example>();
        }
    }
}
