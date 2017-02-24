using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Attributes;
using Lynicon.Extensibility;
using Microsoft.AspNetCore.Mvc.Rendering;
using Lynicon.Utility;

namespace Lynicon.Models
{
    /// <summary>
    /// View model for showing the Filter page
    /// </summary>
    public class ItemListerViewModel
    {
        /// <summary>
        /// The possible content classes as a list of SelectListItems for display to the user
        /// </summary>
        public List<SelectListItem> ContentClasses
        {
            get
            {
                return GetContentTypes()
                    .Select(ct => new SelectListItem { Text = BaseContent.ContentClassDisplayName(ct), Value = ct.FullName })
                    .ToList();
            }
        }

        /// <summary>
        /// Get all the content types together with all their base types and interfaces
        /// </summary>
        /// <returns>All the types</returns>
        public static List<Type> GetContentTypes()
        {
            return ContentTypeHierarchy.AllContentTypes
                    .Concat(ContentTypeHierarchy.ContentSubtypes.Keys)
                    .Distinct()
                    .Where(ct => !ct.IsInterface() || ct.GetCustomAttribute<ContentTypeDisplayNameAttribute>() != null)
                    .ToList();
        }

        /// <summary>
        /// Get all the filters to show on the page
        /// </summary>
        /// 
        public List<Lynicon.Extensibility.ListFilter> Filters
        {
            get
            {
                return LyniconUi.Instance.Filters;
            }
        }
    }
}
