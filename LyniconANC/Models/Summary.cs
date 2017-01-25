using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using Lynicon.Attributes;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Newtonsoft.Json;

namespace Lynicon.Models
{
    [Serializable]
    /// <summary>
    /// This class and its inheritors are marked as available as summary information to pages outside the
    /// page to which the content belongs
    /// </summary>
    public class Summary
    {
        /// <summary>
        /// The title for the content item - used in many UI context to describe the content item
        /// </summary>
        [FieldFilter("Item Title", Showable = false)]
        public string Title { get; set; }

        // These fields are saved but their saved values are overridden with calculated values when getting
        // summaries through specfic summary reading methods.

        /// <summary>
        /// The id unique to this content record (unique to every version)
        /// </summary>
        public object UniqueId { get; set; }

        /// <summary>
        /// The Identity of the content item (common to all versions of the same content item)
        /// </summary>
        [ScaffoldColumn(false)]
        public object Id { get; set; }

        private string url;
        /// <summary>
        /// The url to be used for the content item
        /// </summary>
        [ScaffoldColumn(false)]
        public string Url
        {
            get
            {
                return (string)EventHub.Instance.ProcessEvent("GenerateContentUrl.Relative", this, url).Data;
            }
            set
            {
                url = value;
            }
        }

        /// <summary>
        /// The type of the content item
        /// </summary>
        [ScaffoldColumn(false)]
        public Type Type { get; set; }

        /// <summary>
        /// The version (ItemVersion) of the content item
        /// </summary>
        [ScaffoldColumn(false)]
        public ItemVersion Version { get; set; }

        /// <summary>
        /// The ItemId of the content item
        /// </summary>
        [ScaffoldColumn(false)]
        public ItemId ItemId
        {
            get
            {
                if (Id == null || Type == null)
                    return null;
                return new ItemId(Type, Id);
            }
        }

        /// <summary>
        /// The ItemVersionedId of the content item
        /// </summary>
        [ScaffoldColumn(false)]
        public ItemVersionedId ItemVersionedId
        {
            get
            {
                if (Id == null || Type == null || Version == null)
                    return null;
                return new ItemVersionedId(new ItemId(Type, Id), Version);
            }
        }

        /// <summary>
        /// A display title for the content item
        /// </summary>
        /// <returns>The Title or the Url if no title</returns>
        public string DisplayTitle()
        {
            return string.IsNullOrEmpty(Title) ? Url : Title;
        }
    }
}
