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
    /// Content subtype for an Image
    /// </summary>
    [Serializable]
    public class Image
    {
        /// <summary>
        /// A static property which allows for a method to be set up to post process the url of the image,
        /// taking the Image object itself plus the standard url
        /// </summary>
        public static Func<Image, string, string> PostProcessUrl = null;

        private readonly Func<string> altDefault;

        /// <summary>
        /// The url of the (original) image, before post processing
        /// </summary>
        public string Url { get; set; }

        string alt = null;
        /// <summary>
        /// The alt text for the image
        /// </summary>
        public string Alt
        {
            get
            {
                if (alt == null)
                    return "image";
                else
                    return alt;
            }
            set { alt = value; }
        }

        /// <summary>
        /// For images shown as background images, allows for controlling how centre of image is mapped
        /// to centre of the div: X (horizontal) direction
        /// </summary>
        public int? BackgroundXPc { get; set; }
        /// <summary>
        /// For images shown as background images, allows for controlling how centre of image is mapped
        /// to centre of the div: Y (vertical) direction
        /// </summary>
        public int? BackgroundYPc { get; set; }

        /// <summary>
        /// Create a new image
        /// </summary>
        public Image()
        {
            Url = "";
        }

        /// <summary>
        /// Get the post-processed original url
        /// </summary>
        /// <returns></returns>
        public string ProcessedURL()
        {
            if (PostProcessUrl != null)
                return PostProcessUrl(this, this.Url);
            else
                return this.Url;
        }

        /// <summary>
        /// Get a url for a cropped version of the image
        /// </summary>
        /// <param name="cropsize">The crop size as widthxheight</param>
        /// <returns>Url for cropped version</returns>
        public string CropURL(string cropsize)
        {
            return CropURL(cropsize, null);
        }
        /// <summary>
        /// Get a url for a cropped version of the image with a given url schema
        /// </summary>
        /// <param name="cropsize">The crop size as widthxheight</param>
        /// <param name="schema">The url schema</param>
        /// <returns>Url for cropped version</returns>
        public string CropURL(string cropsize, string schema)
        {
            string strURL = "";

            if (string.IsNullOrEmpty(Url))
                return Url;

            if (!String.IsNullOrEmpty(cropsize))
            {
                List<string> parts = Url.Split('.').ToList();
                if (parts.Count > 1 && Regex.IsMatch(parts[parts.Count - 2], "\\d+[xX]\\d+"))
                    parts[parts.Count - 2] = cropsize;
                else
                    parts.Insert(parts.Count - 1, cropsize);
                strURL = parts.Join(".");
            }
            else
            {
                strURL = Url;
            }

            if (!string.IsNullOrEmpty(schema))
            {
                if (!schema.EndsWith(":"))
                    schema += ":";

                if (Url.StartsWith("//"))
                    strURL = schema + strURL;
                else if (Url.StartsWith("http://"))
                    strURL = schema + strURL.After("http:");
                else if (Url.StartsWith("https://"))
                    strURL = schema + strURL.After("https:");
            }

            if (PostProcessUrl != null)
                strURL = PostProcessUrl(this, strURL);

            return strURL;
        }

        /// <summary>
        /// A background style for when the image is shown as a background using positioning
        /// </summary>
        /// <param name="cropsize">The crop size for the image</param>
        /// <returns>background style lines for the image</returns>
        public string BackgroundStyle(string cropsize)
        {
            string res = string.Format("background-position: {0}% {1}%; ", BackgroundXPc ?? 50, BackgroundYPc ?? 50);
            if (!string.IsNullOrEmpty(Url))
                res += "background-image: url(\"" + CropURL(cropsize) + "\"); ";
            return res;
        }
    }
}
