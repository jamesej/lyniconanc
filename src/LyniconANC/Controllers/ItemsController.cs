using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using Lynicon.Collation;
using Lynicon.Models;
using Lynicon.Repositories;
using Lynicon.Utility;
using Lynicon.Extensibility;
//using Lynicon.Extensions;
using Lynicon.Membership;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Lynicon.Services;

namespace Lynicon.Controllers
{
    /// <summary>
    /// Controller for the Items and Filters CMS pages
    /// </summary>
    [Area("Lynicon")]
    public class ItemsController : Controller
    {
        LyniconSystem sys;

        public ItemsController(LyniconSystem sys)
        {
            this.sys = sys;
        }
        /// <summary>
        /// Serve the List page of all content items by type with search and paging
        /// </summary>
        /// <returns>The List page</returns>
        public IActionResult Index()
        {
            ViewData.Add("UrlPermission", SecurityManager.Current.CurrentUserInRole(Lynicon.Membership.User.EditorRole));
            return View();
        }

        /// <summary>
        /// Serve the Filters page listing content items with filters and operations
        /// </summary>
        /// <returns>The Filters page</returns>
        [Authorize(Roles = Lynicon.Membership.User.EditorRole)]
        public IActionResult List()
        {
            return View();
        }

        public IActionResult Find()
        {
            ViewData.Add("UrlPermission", SecurityManager.Current.CurrentUserInRole(Lynicon.Membership.User.EditorRole));
            return PartialView("LyniconItems");
        }

        /// <summary>
        /// Get markup to refresh an individual item on the List page
        /// </summary>
        /// <param name="datatype">Type of the item</param>
        /// <param name="id">Id of the item</param>
        /// <returns>Markup for the item</returns>
        public IActionResult GetItem(string datatype, string id)
        {
            ViewData.Add("UrlPermission", SecurityManager.Current.CurrentUserInRole(Lynicon.Membership.User.EditorRole));
            Type type = ContentTypeHierarchy.GetContentType(datatype);
            var summ = Collator.Instance.Get<Summary>(new ItemId(type, id));
            return PartialView("ItemListSummary", summ);
        }

        /// <summary>
        /// Get markup to show all the items of a type in a paged box on the List page
        /// </summary>
        /// <param name="datatype">The data type</param>
        /// <returns>Markup of the paged box listing the items</returns>
        public IActionResult GetPage(string datatype)
        {
            ViewData.Add("UrlPermission", SecurityManager.Current.CurrentUserInRole(Lynicon.Membership.User.EditorRole));
            ViewData.Add("DelPermission", SecurityManager.Current.CurrentUserInRole(Lynicon.Membership.User.AdminRole));
            Type type = ContentTypeHierarchy.GetContentType(datatype);
            Type containerType = Collator.Instance.ContainerType(type);
            // invoke Collator.Instance.GetList<Summary, type>(new Type[] { type }, RouteData).ToArray();
            var summs = (IEnumerable<Summary>)ReflectionX.InvokeGenericMethod(Collator.Instance, "GetList", new Type[] { typeof(Summary), containerType }, new Type[] { type }, RouteData);
            var data = summs.ToArray();
            ViewData["ViewName"] = "ItemPage";
            return PartialView("PartialViewContainer", data);
        }

        /// <summary>
        /// Get markup to show the filter based lister
        /// </summary>
        /// <returns>Markup of filter based lister as PartialView</returns>
        public IActionResult ItemLister()
        {
            var u = SecurityManager.Current.User;
            var v = sys.Versions.CurrentVersion;
            ViewData["VersionSelector"] = sys.Versions.SelectionViewModel(u, v);
            return PartialView(new ItemListerViewModel());
        }

        /// <summary>
        /// Get the items to show on the filter based lister given filter values
        /// </summary>
        /// <param name="versionFilter">The filter for the version</param>
        /// <param name="classFilter">The filter for the data type or data types</param>
        /// <param name="filters">The custom filters</param>
        /// <returns>Markup for the paged list of items</returns>

        [Authorize(Policy = "CanEditData")]
        public IActionResult FilterItems(List<string> versionFilter, string[] classFilter, List<ListFilter> filters)
        {
            var pagingSpec = PagingSpec.Create(RouteData.Values);
            if (filters == null)
                filters = new List<ListFilter>();

            var pagedResult = FilterManager.Instance.RunFilter(versionFilter, classFilter, filters, pagingSpec);

            RouteData.DataTokens["@Paging"] = pagingSpec;

            ViewData["ShowFilts"] = filters.Where(f => f.Show).ToList();

            return PartialView(pagedResult);
        }

        /// <summary>
        /// Get the items from the filter based lister as a CSV file
        /// </summary>
        /// <param name="versionFilter">The filter for the version</param>
        /// <param name="classFilter">The filter for the data type or data types</param>
        /// <param name="filters">The custom filters</param>
        /// <returns>A CSV file containing the listed data for all the relevant content items</returns>

        [Authorize(Policy = "CanEditData")]
        public IActionResult FilterCsv(List<string> versionFilter, string[] classFilter, List<ListFilter> filters)
        {
            var pagingSpec = PagingSpec.Create(RouteData.Values);
            if (filters == null)
                filters = new List<ListFilter>();

            string csv = FilterManager.Instance.GenerateCsv(versionFilter, classFilter, filters, pagingSpec);

            return File(Encoding.UTF8.GetBytes(csv), "text/csv", "report.csv");
        }
    }
}
