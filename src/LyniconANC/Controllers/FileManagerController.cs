using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lynicon.Controllers
{
    /// <summary>
    /// Serve the FileManager
    /// </summary>
    [Area("Lynicon")]
    public class FileManagerController : Controller
    {
        /// <summary>
        /// Get the FileManager
        /// </summary>
        /// <returns>HTML page of FileManager</returns>
        public IActionResult Index()
        {
            return View("LyniconFileManager");
        }
    }
}
