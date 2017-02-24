using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Lynicon.Utility;

namespace Lynicon.Attributes
{
    /// <summary>
    /// If a property on a content type appears in the content type's summary type, the
    /// content type property needs to be marked with this attribute.  Works across
    /// inherited properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SummaryAttribute : Attribute
    {
        public string SummaryProperty { get; private set; }

        public SummaryAttribute()
        {
            SummaryProperty = null;
        }
        public SummaryAttribute(string summaryProperty)
        {
            SummaryProperty = summaryProperty;
        }
    }
}
