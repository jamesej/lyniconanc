using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using Lynicon.Collation;
using Lynicon.Routing;
using Lynicon.Map;
using Lynicon.Models;
using Lynicon.Extensibility;
using Lynicon.Modules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Lynicon.Controllers
{
    /// <summary>
    /// Services to analyse urls in various ways
    /// </summary>
    [Area("Lynicon")]
    public class UrlManagerController : Controller
    {
        public IActionResult Index()
        {
            return Content((string)this.RouteData.DataTokens["$urlget"], "text/plain");
        }

        /// <summary>
        /// Get the possible patterns for a new url of the given type
        /// </summary>
        /// <param name="datatype">Type as its name as a string</param>
        /// <returns>The patterns as JSON</returns>
        public IActionResult TypePatterns(string datatype)
        {
            Type type = ContentTypeHierarchy.GetContentType(datatype);
            var patterns = RouteData.GetTemplatePatterns(type);
            return Json(patterns);
        }

        /// <summary>
        /// Move the item whose id is in the query value of the query key $urlset and whose type is given to
        /// the current main url
        /// </summary>
        /// <param name="type">Type of the item to move</param>
        /// <returns>OK when done</returns>
        [HttpPost, Authorize(Roles = Lynicon.Membership.User.EditorRole)]
        public IActionResult MoveUrl()
        {
            Type type = (Type)RouteData.DataTokens["Type"];
            try
            {
                Collator.Instance.MoveAddress(
                    new ItemId(type, (string)this.RouteData.DataTokens["$urlset"]),
                    new Address(type, this.RouteData.GetOriginal()));
            }
            catch (ApplicationException appEx)
            {
                if (appEx.Message == "There is an item already at that address")
                    return Content("Already Exists");
            }
            return Content("OK");
        }

        /// <summary>
        /// Simply returns the message Already Exists (as the result of a divert)
        /// </summary>
        /// <returns>The string "Already Exists"</returns>
        public IActionResult AlreadyExists()
        {
            return Content("Already Exists");
        }

        /// <summary>
        /// Delete a content item
        /// </summary>
        /// <param name="data">the content item to delete</param>
        /// <returns>OK when done</returns>
        [HttpPost, Authorize(Roles = Lynicon.Membership.User.AdminRole)]
        public IActionResult Delete(object data)
        {
            Collator.Instance.Delete(data);
            return Content("OK");
        }

        /// <summary>
        /// Simply returns the message Exists (as the result of a divert)
        /// </summary>
        /// <returns>The string "Exists"</returns>
        public IActionResult VerifyExists()
        {
            return Content("Exists");
        }

    }
}
