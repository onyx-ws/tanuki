using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Onyx.Tanuki.Configuration
{
    public class Example
    {
        public string Name { get; set; }

        /// <summary>
        /// Short description for the example.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Long description for the example. CommonMark syntax MAY be used for rich text representation.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// A URL that points to the literal example. This provides the capability to reference examples that cannot easily be included in JSON or YAML documents. The value field and externalValue field are mutually exclusive.
        /// </summary>
        public string ExternalValue { get; set; }

        /// <summary>
        /// Embedded literal example. The value field and externalValue field are mutually exclusive. To represent examples of media types that cannot naturally represented in JSON or YAML, use a string value to contain the example, escaping where necessary.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Fetches sample from external resource
        /// </summary>
        /// <param name="url">The absolute URL of the sample file</param>
        public void FetchExternalValue(string url)
        {
            try
            {
                using HttpClient client = new HttpClient();
                Value = client.GetStringAsync(url).Result;
                ExternalValue = url;
            }
            catch { }
        }
    }
}