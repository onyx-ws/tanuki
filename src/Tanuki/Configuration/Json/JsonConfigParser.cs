using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Onyx.Tanuki.Configuration.Json
{
    public class JsonConfigParser
    {
        public static Tanuki Parse(string json)
        {
            using var jDocument = JsonDocument.Parse(json);
            var jTanuki = jDocument.RootElement;

            Tanuki tanuki = new Tanuki();
            tanuki.Paths = JsonPathsParser.Parse(jTanuki);

            return tanuki;
        }
    }
}
