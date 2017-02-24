using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lynicon.Attributes
{
    /// <summary>
    /// Can be attached to action methods to specify that they do not return a page so that when the action method name
    /// is included in a url, this method is not considered one of the possible values
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class NonPageAttribute : Attribute
    {
        public NonPageAttribute()
        {
        }
    }
}
