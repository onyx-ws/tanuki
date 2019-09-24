using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Onyx.Tanuki.Configuration.Json
{
    public class JsonConfiguration
    {
        public Tanuki Tanuki { get; set; }

        public JsonConfiguration()
        {
            var json = File.ReadAllText("./tanuki.json");
            Tanuki = JsonConfigParser.Parse(json);
        }
    }
}