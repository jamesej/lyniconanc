using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Lynicon.Utility;

namespace Lynicon.Attributes
{
    /// <summary>
    /// The name that should be shown to the user when the name of this content type appears in the Lynicon UI
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class ContentTypeDisplayNameAttribute : Attribute
    {
        /// <summary>
        /// The name to show for this type in the Lynicon UI
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// Construct the content type display name attribute
        /// </summary>
        /// <param name="displayName">The name to show for this type in the Lynicon UI</param>
        public ContentTypeDisplayNameAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
