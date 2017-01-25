using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// Description of how to display a version selector
    /// </summary>
    public class VersionSelectionViewModel
    {
        /// <summary>
        /// The title of the version element
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The version key for this version element
        /// </summary>
        public string VersionKey { get; set; }
        /// <summary>
        /// CSS class to apply to display of this version element
        /// </summary>
        public string CssClass { get; set; }
        /// <summary>
        /// A list of SelectListItems which could be used for a drop down for this version element
        /// </summary>
        public List<SelectListItem> SelectList { get; set; }
    }
}
