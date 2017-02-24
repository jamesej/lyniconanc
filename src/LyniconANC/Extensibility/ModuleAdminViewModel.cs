using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// Base class for describing a module in the Admin page
    /// </summary>
    public class ModuleAdminViewModel
    {
        /// <summary>
        /// The view name to use for rendering the module details
        /// </summary>
        public string ViewName { get; set; }
        /// <summary>
        /// The title/name of the module
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Details of any error that was thrown initialising the module
        /// </summary>
        public string Error { get; set; }
    }
}
