using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Lynicon.Models
{
    /// <summary>
    /// Any content which represents a web page should inherit from this class
    /// </summary>
    [Serializable]
    public class PageContent : BaseContent
    {
        /// <summary>
        /// The value for the title tag
        /// </summary>
        public string PageTitle { get; set; }

        /// <summary>
        /// The value for the meta description
        /// </summary>
        [UIHint("Multiline")]
        public string PageDescription { get; set; }

        /// <summary>
        /// Get the unique id of the content item (not the Identity)
        /// </summary>
        /// <returns>the unique id</returns>
        public object GetId()
        {
            return this.OriginalRecord.Id;
        }

        /// <summary>
        /// Create a new PageContent
        /// </summary>
        public PageContent()
        {
        }

    }
}
