using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Lynicon.Utility;

namespace Lynicon.Attributes
{
    /// <summary>
    /// Attach this attribute to a class to indicate its summary type
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SummaryTypeAttribute : Attribute
    {
        /// <summary>
        /// The summary type of the content type to which this is attached
        /// </summary>
        public Type SummaryType { get; private set; }

        public SummaryTypeAttribute(Type summaryType)
        {
            SummaryType = summaryType;
        }
    }
}
