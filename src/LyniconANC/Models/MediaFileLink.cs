using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lynicon.Models
{
    /// <summary>
    /// Content subtype for linking to a media file
    /// </summary>
    [Serializable]
    public class MediaFileLink
    {
        /// <summary>
        /// The url of the media file
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// The content of the link to the media file
        /// </summary>
        public BbText Content { get; set; }

        public MediaFileLink()
        {
            Url = "";
        }
    }
}
