using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Models;
using Lynicon.Repositories;
using Lynicon.Utility;
using Microsoft.AspNetCore.Mvc;
using Lynicon.Modules;

namespace Lynicon.Controllers
{
    /// <summary>
    /// Controller for maintenance of the Full cache
    /// </summary>
    [Area("Lynicon")]
    public class ContentSchemaController : Controller
    {
        /// <summary>
        /// Write out the content schema from memory to the file
        /// </summary>
        /// <returns>OK when done</returns>
        public IActionResult WriteToFile()
        {
            ((ContentSchemaModule)LyniconModuleManager.Instance.Modules["ContentSchema"]).Dump();
            return Content("OK");
        }
    }
}
