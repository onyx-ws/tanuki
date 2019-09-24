using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Onyx.Tanuki.Configuration
{
    /// <summary>
    /// Describes a single response from an API Operation, including design-time, static links to operations based on the response
    /// </summary>
    public class Response
    {
        /// <summary>
        /// The HTTP status code used to describe the expected response HTTP status code. A range of response codes can be represented using the uppercase wildcard character X
        /// </summary>
        public string StatusCode { get; set; }

        /// <summary>
        /// A short description of the response. CommonMark syntax MAY be used for rich text representation
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// A map containing descriptions of potential response payloads
        /// </summary>
        public List<Content> Content { get; set; }
    }
}
