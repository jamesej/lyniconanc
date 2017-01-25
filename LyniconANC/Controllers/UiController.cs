using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Web.UI;
using Lynicon.Collation;
using Lynicon.Routing;
using Lynicon.Map;
using Lynicon.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lynicon.Controllers
{
    /// <summary>
    /// General UI webservices
    /// </summary>
    [Area("Lynicon")]
    public class UiController : Controller
    {
        /// <summary>
        /// Show the function panel reveal panel
        /// </summary>
        /// <returns>Markup for the reveal panel</returns>
        public IActionResult FunctionReveal()
        {
            return PartialView();
        }
    }
}
