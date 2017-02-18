using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Lynicon.Models
{
    /// <summary>
    /// Content subtype for a hyperlink
    /// </summary>
    [Serializable]
    public class Link
    {
        /// <summary>
        /// Url for the link
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Text content for the link
        /// </summary>
        public BbText Content { get; set; }

        /// <summary>
        /// Create a new link
        /// </summary>
        public Link()
        {
            Content = "";
        }
    }
}
