using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Web.UI;
using Lynicon.Attributes;
using Microsoft.AspNetCore.Mvc;

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
            WebResourceAttribute attribute = assembly.GetCustomAttributes(true)
                .OfType<WebResourceAttribute>()
                .FirstOrDefault(wra => wra.WebResource == location);
            string contentType = (attribute == null ? "text/plain" : attribute.ContentType);

            Stream stream = assembly.GetManifestResourceStream(location);

            return File(stream, contentType);
        }
    }
}
