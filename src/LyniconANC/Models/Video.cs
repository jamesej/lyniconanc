using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Attributes;

namespace Lynicon.Models
{
    /// <summary>
    /// Content subtype for a video
    /// </summary>
    [Serializable]
    public class Video
    {
        /// <summary>
        /// Embed string for markup to show the video
        /// </summary>
        public string Embed { get; set; }
        /// <summary>
        /// Url for an HTML5 video
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// Url for an HTML5 video when shown on mobile
        /// </summary>
        public string MobileUrl { get; set; }
        /// <summary>
        /// Poster (still thumbnail image) for an HTML5 video
        /// </summary>
        public Image Poster { get; set; }

        /// <summary>
        /// The duration of the video in seconds
        /// </summary>
        public int? Duration { get; set; }

        /// <summary>
        /// Create a new Video
        /// </summary>
        public Video()
        {
            Poster = new Image();
        }
    }
}
