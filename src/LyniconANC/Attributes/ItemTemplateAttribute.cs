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
    /// When attached to a collection-typed property, specifies that the items in the collection should
    /// use the editor template specified
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ItemTemplateAttribute : Attribute, IMetadataAware
    {
        public const string Key = "_ItemTemplate";

        /// <summary>
        /// The editor template view name for the items in the collection to which this is attached
        /// </summary>
        public string ItemTemplate { get; set; }

        /// <summary>
        /// Create a new item template attribute
        /// </summary>
        /// <param name="itemTemplate">The editor template view name for the items in the collection to which this is attached</param>
        public ItemTemplateAttribute(string itemTemplate)
        {
            ItemTemplate = itemTemplate;
        }

        public void OnMetadataCreated(DisplayMetadataProviderContext context)
        {
            context.DisplayMetadata.AdditionalValues.Add(Key, this);
        }
    }
}
