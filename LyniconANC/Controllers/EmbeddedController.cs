using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using Lynicon.Attributes;
using Microsoft.AspNetCore.Mvc;
using Lynicon.Utility;

namespace Lynicon.Controllers
{
    /// <summary>
    /// Serves files embedded in the assembly
    /// </summary>
    //[CompressContent]
    [Area("Lynicon")]
    public class EmbeddedController : Controller
    {
        /// <summary>
        /// Serve a file embedded in the current assembly
        /// </summary>
        /// <param name="innerUrl">The inner url of the file</param>
        /// <returns>The file</returns>
        //[OutputCache(Location = OutputCacheLocation.Client, Duration = 36000)]
        public IActionResult Index(string innerUrl)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            if (innerUrl.EndsWith("/"))
                innerUrl = innerUrl.Substring(0, innerUrl.Length - 1);
            string location = "Lynicon." + innerUrl.Replace("/", ".");
            string contentType = "text/plain";
            string extension = location.LastAfter(".").ToLower();
            switch (extension)
            {
                case "jpg":
                case "jpeg":
                    contentType = "image/jpeg";
                    break;
                case "gif":
                    contentType = "image/gif";
                    break;
                case "png":
                    contentType = "image/png";
                    break;
                case "js":
                    contentType = "application/javascript";
                    break;
                case "css":
                    contentType = "text/css";
                    break;
            }

            Stream stream = assembly.GetManifestResourceStream(location);

            return File(stream, contentType);
        }
    }
}
