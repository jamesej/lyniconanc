using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lynicon.Attributes
{
    /// <summary>
    /// Marks a container property as not part of the summarised version, i.e. not required for generating the summary
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class NotSummarisedAttribute : Attribute
    {

    }
}
