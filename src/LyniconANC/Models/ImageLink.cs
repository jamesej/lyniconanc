using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Lynicon.Utility;

namespace Lynicon.Models
{
    /// <summary>
    /// Content subtype for an Image with a Link
    /// </summary>
    
    public class ImageLink
    {
        /// <summary>
        /// The image
        /// </summary>
        public Image Image { get; set; }

        /// <summary>
        /// The link
        /// </summary>
        public Link Link { get; set; }

        /// <summary>
        /// Create a new image
        /// </summary>
        public ImageLink()
        {
            Image = new Image();
            Link = new Link();
        }
    }
}
