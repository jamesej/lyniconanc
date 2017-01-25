using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Lynicon.Utility;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Lynicon.Attributes
{
    /// <summary>
    /// The CMS Editor collapsible block in which this property should appear
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class DisplayBlockAttribute : Attribute
    {
        /// <summary>
        /// The title shown on the collapsible block
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Display ordering for this block; default is 0
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Create a display block attribute that just sets the ordering for the block in which this property appears
        /// </summary>
        /// <param name="order"></param>
        public DisplayBlockAttribute(int order)
        {
            Title = null;
            Order = order;
        }
        /// <summary>
        /// Create a display block attribute setting the title of the block in which this property appears and its ordering
        /// </summary>
        /// <param name="title">The title of the block</param>
        /// <param name="order">The ordering of the block; default is 0</param>
        public DisplayBlockAttribute(string title, int order)
        {
            Title = title;
            Order = order;
        }
        /// <summary>
        /// Create a display block attribute setting the title of the block in which this property appears with default ordering
        /// </summary>
        /// <param name="title">The title of the block</param>
        public DisplayBlockAttribute(string title)
        {
            Title = title;
            Order = 0;
        }

        #region IMetadataAware Members

        public void OnMetadataCreated(DisplayMetadataProviderContext context)
        {
            context.DisplayMetadata.AdditionalValues.Add("DisplayBlock", this);
        }

        #endregion
    }
}
