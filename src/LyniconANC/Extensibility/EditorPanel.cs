using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// Represents a custom panel in the editor
    /// </summary>
    public class EditorPanel : IRolesRequired
    {
        /// <summary>
        /// Title of the panel as displayed
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// String containing list of all role letters required to view the panel, it
        /// is not displayed otherwise
        /// </summary>
        public string RequiredRoles { get; set; }
        /// <summary>
        /// Function returning true if the content panel should be displayed for the
        /// argument which is a Type
        /// </summary>
        public Func<Type, bool> ContentTypeSelector { get; set; }
        /// <summary>
        /// The View name for the view used to display the panel contents
        /// </summary>
        public string ViewName { get; set; }
        /// <summary>
        /// The order position in which to show the panel with relation to the other
        /// editor panels
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Create a new EditorPanel
        /// </summary>
        public EditorPanel()
        {
            Order = 0;
            ContentTypeSelector = t => false;
        }
    }
}
