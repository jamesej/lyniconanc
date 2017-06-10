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
using Lynicon.Repositories;
using Lynicon.Utility;
using Newtonsoft.Json.Linq;
using Lynicon.Extensibility;
using Lynicon.Modules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Lynicon.DataSources;
using Lynicon.Services;

namespace Lynicon.Controllers
{
    /// <summary>
    /// Controller for the admin page
    /// </summary>
    [Area("Lynicon")]
    public class AdminController : Controller
    {
        /// <summary>
        /// Show the admin page
        /// </summary>
        /// <returns>The admin page</returns>
        [Authorize(Roles = Membership.User.AdminRole)]
        public IActionResult Index()
        {
            var avm = new AdminViewModel();
            return View(avm);
        }

        /// <summary>
        /// Move data from one property to another across all instances of a content item type,
        /// these may be on summary or main part of the item
        /// </summary>
        /// <param name="dataType">The name of the data type, the type name or the full namespace name</param>
        /// <param name="path">The property path to read from</param>
        /// <param name="pathInSummary">Whether the read property is in the summary</param>
        /// <param name="newPath">The property path to write to</param>
        /// <param name="newPathInSummary">Whether the write property is in the summary</param>
        /// <param name="isDelete">Whether to simply delete the property instead of copying it</param>
        /// <returns>status result</returns>
        [HttpPost, Authorize(Roles = Membership.User.AdminRole)]
        public IActionResult Rename(string dataType, string path, bool pathInSummary, string newPath, bool newPathInSummary, bool? isDelete)
        {
            Type contentType = ContentTypeHierarchy.GetContentType(dataType);
            if (contentType == null)
                return Content("No such type, remember to add 'Content' where necessary");
            int propMissing = 0;
            int updated = 0;

            // create a new ContentRepository so we can bypass any block to writing caused by existing data problems
            // on the global data api
            var repo = new ContentRepository(new CoreDataSourceFactory(LyniconSystem.Instance));
            repo.BypassChangeProblems = true;

            foreach (ContentItem ci in repo.Get<ContentItem>(contentType, new Type[] { contentType }, iq => iq))
            {
                JObject jObjFrom;
                if (pathInSummary)
                    jObjFrom = JObject.Parse(ci.Summary ?? "{}");
                else
                    jObjFrom = JObject.Parse(ci.Content ?? "{}");

                JObject jObjTo = null;
                bool doDelete = isDelete ?? false;
                bool wasComputed = false;
                if (!doDelete)
                {
                    if (newPathInSummary)
                        jObjTo = JObject.Parse(ci.Summary ?? "{}");
                    else
                        jObjTo = JObject.Parse(ci.Content ?? "{}");
                    
                    jObjTo.CopyPropertyFrom(newPath, jObjFrom, path);
                    JProperty prop = jObjTo.PropertyByPath(path);

                    // try to deserialize in case its a computed property
                    if (!pathInSummary && newPathInSummary && prop == null)
                    {
                        object content = jObjFrom.ToObject(contentType);
                        object val = ReflectionX.GetPropertyValueByPath(content, path);
                        if (val != null)
                        {
                            string pName = newPath.Contains(".") ? newPath.LastAfter(".") : newPath;
                            jObjTo.AddAtPath(newPath, new JProperty(pName, val));
                            wasComputed = true;
                        }
                    }

                    if (pathInSummary == newPathInSummary)
                    {

                        if (prop != null)
                        {
                            prop.Remove();
                            updated++;
                        }
                        else
                            propMissing++;
                    }
                }

                if (pathInSummary != newPathInSummary || doDelete) // we need to update both summary and content
                {
                    // remove the old path
                    JProperty prop = jObjFrom.PropertyByPath(path);
                    if (prop != null)
                    {
                        prop.Remove();
                        updated++;
                        
                        if (pathInSummary)
                            ci.Summary = jObjFrom.ToString();
                        else
                            ci.Content = jObjFrom.ToString();
                    }
                    else if (wasComputed)
                        updated++;
                    else
                        propMissing++;
                }

                if (!doDelete)
                {
                    if (newPathInSummary)
                        ci.Summary = jObjTo.ToString();
                    else
                        ci.Content = jObjTo.ToString();
                }

                repo.Set(new List<object> { ci }, new Dictionary<string, object>());
            }

            return Content(string.Format("{0} updated {1} had property missing", updated, propMissing));
        }

        /// <summary>
        /// Mark a data problem as being resolved
        /// </summary>
        /// <param name="Id">The Guid identifier of the data problem</param>
        /// <returns>The status result of the operation</returns>
        [HttpPost, Authorize(Roles = Membership.User.AdminRole)]
        public IActionResult ResolveProblem(Guid Id)
        {
            var problem = ContentRepository.ChangeProblems.FirstOrDefault(cp => cp.Id == Id);
            if (problem == null)
                return Content("No such problem");

            ContentRepository.ChangeProblems.Remove(problem);

            // Resave schema with resolved problem
            var schemaModule = LyniconModuleManager.Instance.GetModule<ContentSchemaModule>();
            if (schemaModule != null)
                schemaModule.Dump();

            return Content("OK");
        }

        /// <summary>
        /// Run a scan to ensure all references point to existing items
        /// </summary>
        /// <returns>The output details of the process</returns>
        [Authorize(Roles = Membership.User.AdminRole)]
        public IActionResult ScanReferences()
        {
            List<string> errors = new List<string>();
            foreach (Type t in ContentTypeHierarchy.AllContentTypes)
            {
                List<string> refErrors = (List<string>)ReflectionX.InvokeGenericMethod(this, "GetReferenceErrors", t);
                errors.AddRange(refErrors);
            }

            return PartialView(errors);
        }

        /// <summary>
        /// Run reference checking on a specific type
        /// </summary>
        /// <typeparam name="T">the Type</typeparam>
        /// <returns>The list of errors</returns>
        public List<string> GetReferenceErrors<T>() where T : class
        {
            var refChecker = new ContentReferenceChecker();
            foreach (object item in Collator.Instance.Get<T>())
            {
                refChecker.ItemTitle = Collator.Instance.GetSummary<Summary>(item).Title;
                refChecker.Visit(item);
            }
            return refChecker.Errors.Select(s => typeof(T).Name + ": " + s).ToList();
        }
    }
}
