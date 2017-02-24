using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Map;
using Lynicon.Models;
using Lynicon.Routing;
using Lynicon.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authorization;
using Lynicon.Services;
using System.Reflection;

namespace Lynicon.Editors
{
    /// <summary>
    /// Shared functionality for editor controllers
    /// </summary>
    public class EditorController : Controller
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(EditorController));

        /// <summary>
        /// Ensure that the editor is available - the internet is connected and you have authorization
        /// </summary>
        /// <returns></returns>
        public IActionResult Ping()
        {
            return Content("OK");
        }

        /// <summary>
        /// Take the data in the form collection and overwrite changed values in the original data
        /// </summary>
        /// <param name="data">The original data (freshly obtained)</param>
        protected async Task Bind(object data)
        {
            new ContentBindingPreparer().Visit(data);
            bool updated = await this.TryUpdateModelAsync(data, data.GetType(), "");
        }

        /// <summary>
        /// Set ViewBag values for rendering the form
        /// </summary>
        protected virtual void SetViewBag()
        {
            ViewBag.OriginalAction = this.RouteData.Values["originalAction"];
            ViewBag.OriginalController = this.RouteData.Values["originalController"];
            ViewBag.OriginalArea = this.RouteData.DataTokens["originalArea"];
            string qString = this.Request.QueryString.ToString();
            if (!string.IsNullOrEmpty(qString) && qString.StartsWith("?"))
                qString = "&" + qString.Substring(1);
            ViewBag.OriginalQuery = qString;
            ViewBag.FileManagerRoot = LyniconSystem.Instance.Settings.FileManagerRoot;
        }

        /// <summary>
        /// Whether we are creating a new content item, as recorded in the RouteData by DataRoute
        /// </summary>
        /// <returns>True if we are creating a new item, false if amending</returns>
        protected bool? GetIfCreate()
        {
            return RouteData.DataTokens.ContainsKey("LynNewItem") ? (bool)RouteData.DataTokens["LynNewItem"] : false;
        }

        /// <summary>
        /// Save the edited data to the api, with a check to see whether the data implies what url it exists on
        /// and whether this has changed, in which case redirect to the new url
        /// </summary>
        /// <param name="data">Updated/created data to save</param>
        /// <returns>Null or the url if we need to redirect</returns>
        protected string SaveWithRedirect(object data)
        {
            RouteData rdOrig = RouteData.GetOriginal();

            string redirectUrl = null;

            // If address implied by address-mapped fields has changed, navigate to new address where item is now found
            var currAddress = new Address(data.GetType(), rdOrig);
            var newAddress = new Address(data);
            if (currAddress.Any(kvp => newAddress.ContainsKey(kvp.Key)
                                && newAddress[kvp.Key].ToString() != currAddress.GetAsString(kvp.Key)))
            {
                redirectUrl = ContentMap.Instance.GetUrl(data);
                EventHub.Instance.ProcessEvent("Content.Move", this, Tuple.Create(rdOrig, data));
            }

            try
            {
                var create = GetIfCreate();
                if ((create ?? false) && ContentMap.Instance.AddressOccupied(currAddress))
                    throw new LyniconUpdateException("There is an item already at this url");

                Collator.Instance.Set(currAddress, data, create);
            }
            catch (LyniconUpdateException lux)
            {
                ModelState.AddModelError("updateFail", lux.Message);
            }

            return redirectUrl;
        }

        /// <summary>
        /// Save without check for redirect
        /// </summary>
        /// <param name="data">Content item data as updated / created</param>
        protected virtual void Save(object data)
        {
            RouteData rdOrig = RouteData.GetOriginal();
            var currAddress = new Address(data.GetType(), rdOrig);

            try
            {
                Collator.Instance.Set(currAddress, data, GetIfCreate());
            }
            catch (LyniconUpdateException lux)
            {
                ModelState.AddModelError("updateFail", lux.Message);
            }
        }

        protected void DoEditAction(object data, string editAction)
        {
            IList list;
            Type itemType;
            switch (editAction.UpTo("-"))
            {
                case "add":
                    list = ReflectionX.GetPropertyValueByPath(data, editAction.After("-")) as IList;
                    itemType = list.GetType().GetGenericArguments()[0];
                    if (list != null)
                        list.Add(CreateInstance(itemType));
                    break;
                case "del":
                    list = ReflectionX.GetPropertyValueByPath(data, editAction.After("-").UpToLast("[")) as IList;
                    itemType = list.GetType().GetGenericArguments()[0];
                    if (list != null)
                    {
                        ModelState.Clear(); // templating system will take old values out of the ModelState unless you do this
                        list.RemoveAt(int.Parse(editAction.LastAfter("[").UpTo("]")));
                    }
                    break;
            }
        }

        /// <summary>
        /// Create a new instance of a type for the editor system
        /// </summary>
        /// <param name="t">The type to create an instance of</param>
        /// <returns>The instance of the type</returns>
        protected object CreateInstance(Type t)
        {
            switch (t.FullName)
            {
                case "System.String":
                    return "-new-"; // an empty string will be converted to null which has no type and will break editor builder
                default:
                    var o = Activator.CreateInstance(t);
                    BaseContent.InitialiseProperties(o);
                    return o;
            }
        }

        /// <summary>
        /// Get the HTML for a new item editor to be added to a list when the '+' button is clicked
        /// </summary>
        /// <param name="data">The full data of the current content item</param>
        /// <param name="propertyPath">The path to the list type property which needs a new item added</param>
        /// <param name="depth">The nesting depth at which the list exists</param>
        /// <param name="pathPrefix">Deprecated method to add a prefix to the property path for list editing</param>
        /// <returns></returns>
        [HttpGet, Authorize(Policy = "CanEditData")]
        public virtual IActionResult PropertyItemHtml(object data, string propertyPath, int depth, string pathPrefix)
        {
            ViewData["propertyPath"] = (pathPrefix ?? "") + propertyPath;
            ViewData["addDepth"] = depth - 1;
            string parentPath = propertyPath.Contains(".") ? propertyPath.UpToLast(".") : "";
            string propertyName = (propertyPath.Contains(".") ? propertyPath.LastAfter(".") : propertyPath).UpTo("[");
            Type parentType = ReflectionX.GetPropertyTypeByPath(data.GetType(), parentPath);
            IList list = ReflectionX.GetPropertyValueByPath(data, propertyPath, true) as IList;
            var listProp = ReflectionX.GetPropertyByPath(data.GetType(), propertyPath);
            Type listType = listProp.PropertyType;
            if (listType.GetType().IsArray)
            {
                list = (IList)Array.CreateInstance(ReflectionX.ElementType(listType), 1);
                list[0] = CreateInstance(listType.GetElementType());
            }
            else
            {
                list = (IList)Activator.CreateInstance(listType);
                list.Add(CreateInstance(ReflectionX.ElementType(listType)));
            }

            ViewData["list"] = list;
            // NEW speculative whether this will work
            var metadata = new EmptyModelMetadataProvider().GetMetadataForProperty(parentType, propertyName);
            ViewData["CollectionAdditionalValues"] = metadata.AdditionalValues;

            RouteData.DataTokens.Add("CancelProcessingHtml", true);
            return PartialView(LyniconSystem.Instance.GetViewPath("LyniconPropertyItem.cshtml"), data);
        }
    }
}
