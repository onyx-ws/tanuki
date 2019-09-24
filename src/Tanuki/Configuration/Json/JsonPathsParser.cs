using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Onyx.Tanuki.Configuration.Json
{
    public class JsonPathsParser
    {
        public static List<Path> Parse(JsonElement tanuki)
        {
            JsonElement jPaths;
            if(tanuki.TryGetProperty("paths", out jPaths))
            {
                List<Path> paths = new List<Path>();

                foreach (JsonProperty jPath in jPaths.EnumerateObject())
                {
                    paths.Add(ParsePath(jPath));
                }

                return paths;
            }
            else
            {
                throw new Exception("No paths defined");
            }
        }

        private static Path ParsePath(JsonProperty jPath)
        {
            Path path = new Path
            {
                Uri = jPath.Name,
                Operations = new List<Operation>()
            };

            foreach (JsonProperty property in jPath.Value.EnumerateObject())
            {
                if(property.Name == "get") path.Operations.Add(JsonOperationParser.Parse(property));
                if(property.Name == "put") path.Operations.Add(JsonOperationParser.Parse(property));
                if(property.Name == "post") path.Operations.Add(JsonOperationParser.Parse(property));
                if(property.Name == "delete") path.Operations.Add(JsonOperationParser.Parse(property));
                if(property.Name == "options") path.Operations.Add(JsonOperationParser.Parse(property));
                if(property.Name == "head") path.Operations.Add(JsonOperationParser.Parse(property));
                if(property.Name == "patch") path.Operations.Add(JsonOperationParser.Parse(property));
                if(property.Name == "trace") path.Operations.Add(JsonOperationParser.Parse(property));
            }

            return path;
        }
    }
}