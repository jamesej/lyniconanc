using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lynicon.Attributes
{
    /// <summary>
    /// Marks a property or class as not to be included in type compositing (but used in entity framework elsewhere)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property|AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class NonCompositeAttribute : Attribute
    {

    }
}
