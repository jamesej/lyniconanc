using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Extensions
{
    /// <summary>
    /// An arbitrary registration of the need for an included script, css or html
    /// </summary>
    public class IncludeEntry
    {
        /// <summary>
        /// in precedence order, later items mutually depend on earlier
        /// </summary>
        public List<string> Dependencies = new List<string>();
        
        /// <summary>
        /// The file to include or literal script, css or html
        /// </summary>
        public string Include { get; set; }

        string id = null;
        /// <summary>
        /// Identifier to ensure this include is included once only
        /// </summary>
        public string Id
        {
            get { return id; }
            set { id = value; }
        }
    }
}
