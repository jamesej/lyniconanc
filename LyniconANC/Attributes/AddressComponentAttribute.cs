using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Lynicon.Utility;

namespace Lynicon.Attributes
{
    /// <summary>
    /// Indicates a property of a content container is used as part of its address
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class AddressComponentAttribute : Attribute
    {
        /// <summary>
        /// The route key (e.g. for "{_0}" in the route's url pattern the route key is "_0") to which the property maps
        /// </summary>
        public string RouteKey { get; set; }
        /// <summary>
        /// A string.Format conversion string to modify how the value appears in the url
        /// </summary>
        public string ConversionFormat { get; set; }
        /// <summary>
        /// If true, use this property as the whole 'path' e.g. &-separated list of url components
        /// </summary>
        public bool UsePath { get; set; }

        public AddressComponentAttribute()
        {
            this.RouteKey = null;
            this.UsePath = false;
        }
        public AddressComponentAttribute(string routeKey)
        {
            this.RouteKey = routeKey;
            this.UsePath = false;
        }
    }
}
