using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Lynicon.Utility;

namespace Lynicon.Attributes
{
    /// <summary>
    /// Used to indicate which heading this content type should come under on the /lynicon/items page
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class ContentTypeCategoryAttribute : Attribute
    {
        public static string GetForType(Type t)
        {
            var ctca = t.GetCustomAttribute<ContentTypeCategoryAttribute>();
            if (ctca != null)
                return ctca.Category;
            var ctci = t.GetInterfaces().FirstOrDefault(i => i.GetCustomAttribute<ContentTypeCategoryAttribute>() != null);
            if (ctci != null)
                return ctci.GetCustomAttribute<ContentTypeCategoryAttribute>().Category;
            return null;
        }

        public string Category { get; private set; }

        /// <summary>
        /// Create a content type category attribute
        /// </summary>
        /// <param name="category">The category the content type this is attached to will appear under on the /lynicon/items page</param>
        public ContentTypeCategoryAttribute(string category)
        {
            Category = category;
        }
    }
}
