using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lynicon.Attributes
{
    /// <summary>
    /// Marks a relational property so that the data API always fetches its contents on an initial read
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class AlwaysIncludeAttribute : Attribute
    {

    }
}
