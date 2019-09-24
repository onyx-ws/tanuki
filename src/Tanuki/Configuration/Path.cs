using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Onyx.Tanuki.Configuration
{
    /// <summary>
    /// Describes the operations available on a single path. A Path Item MAY be empty, due to ACL constraints. The path itself is still exposed to the documentation viewer but they will not know which operations and parameters are available.
    /// </summary>
    public class Path
    {
        /// <summary>
        /// The relative path to an individual endpoint. The field name begins with a slash
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// The list of operations that can be performed on this path
        /// </summary>
        public List<Operation> Operations { get; set; }
    }
}
