using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Onyx.Tanuki.Configuration.Json
{
    public class JsonExampleParser
    {
        public static Example Parse(JsonProperty json)
        {
            Example example = new Example()
            {
                Name = json.Name
            };

            foreach (JsonProperty property in json.Value.EnumerateObject())
            {
                if (property.Name == "summary") example.Summary = property.Value.GetString();
                if (property.Name == "value") example.Value = property.Value.GetString();
                if (property.Name == "externalValue") example.FetchExternalValue(property.Value.GetString());
            }
            return example;
        }
    }
}
