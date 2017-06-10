using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Membership;
using Lynicon.Repositories;
using Lynicon.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Models
{
    /// <summary>
    /// Handles running filtering operations and returning the results
    /// </summary>
    public class FilterManager
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(FilterManager));

        static readonly FilterManager instance = new FilterManager();
        public static FilterManager Instance { get { return instance; } }

        static FilterManager() { }

        /// <summary>
        /// Gets all the containers which contain one of a list of content types and which satisfy a list of filters
        /// </summary>
        /// <typeparam name="T">A type which can be assigned all the resulting containers</typeparam>
        /// <param name="contentTypes">The content types which are allowed</param>
        /// <param name="containerFilters">The filters which operate on a container which the returned containers must satisfy</param>
        /// <returns>The filtered list of containers</returns>
        public IEnumerable<object> GetFilteredContainers<T>(List<Type> contentTypes, List<ListFilter> containerFilters) where T : class
        {
            Func<IQueryable<T>, IQueryable<T>> queryBody = iq => iq;
            foreach (ListFilter filt in containerFilters)
            {
                var thisQry = queryBody; // local var is necessary to create closure variable whose value doesn't change
                var thisFilt = filt;
                queryBody = iq => thisFilt.Apply<T>()(thisQry(iq));
            }

            // The repository calls below will hit the summary cache if it is running
            IEnumerable<object> conts;
            if (ContentTypeHierarchy.AllContentTypes.Contains(typeof(T)))
            {
                conts = Repository.Instance.Registered(typeof(T)).Get<T>(typeof(Summary), contentTypes, queryBody);
            }
            else
                conts = Repository.Instance.Get<T>(typeof(Summary), contentTypes, queryBody);

            return conts;
        }

        /// <summary>
        /// Filter a list of containers using summary filters
        /// </summary>
        /// <typeparam name="T">A summary type to which the summaries of all the containers can be assigned</typeparam>
        /// <param name="containers">List of containers</param>
        /// <param name="summaryFilters">List of filters which operate on summaries</param>
        /// <returns></returns>
        public List<Tuple<object, Summary>> FilterContainers<T>(IEnumerable<object> containers, List<ListFilter> summaryFilters) where T : Summary
        {
            Func<IQueryable<T>, IQueryable<T>> queryBody = iq => iq;
            foreach (ListFilter filt in summaryFilters)
            {
                var thisQry = queryBody; // local var is necessary to create closure variable whose value doesn't change
                var thisFilt = filt;
                queryBody = iq => thisFilt.Apply<T>()(thisQry(iq));
            }

            var summDict = containers
                .Select(c => Tuple.Create(c, Collator.Instance.GetSummary<Summary>(c) as T))
                .Where(t => t.Item2 != null)
                .ToDictionary(t => t.Item2.Id, t => t, t => Debug.WriteLine("dup " + t.Item2.ItemId));

            var results0 = queryBody(summDict.Values
                .Select(t => t.Item2)
                .AsQueryable())
                .ToList();

            var results = results0
                .Select(s => { var t = summDict[s.Id]; return Tuple.Create(summDict[s.Id].Item1, summDict[s.Id].Item2 as Summary); })
                .OrderBy(tp => tp.Item2.Title)
                .ToList();

            return results;
        }

        /// <summary>
        /// Get a list of container x summary tuples where the content is one of the a list of content types, filtered by container and
        /// summary filters
        /// </summary>
        /// <param name="contentTypes">The allowed content types</param>
        /// <param name="containerFilters">The filters operating on containers</param>
        /// <param name="summaryFilters">The filters operating on summaries</param>
        /// <returns></returns>
        public List<Tuple<object, Summary>> GetFilterSummaries(List<Type> contentTypes, List<ListFilter> containerFilters, List<ListFilter> summaryFilters)
        {
            // Containers

            var showContFields = new List<PropertyInfo>();
            Type containerType = typeof(object);
            // find most general common type of all ApplicableTypes of filters.  If none exists, return empty list.
            foreach (var filt in containerFilters.Where(f => f.Active))
                if (containerType.IsAssignableFrom(filt.ApplicableType))
                    containerType = filt.ApplicableType;
                else
                    return new List<Tuple<object, Summary>>();

            if (contentTypes == null || contentTypes.Count == 0)
                contentTypes = ContentTypeHierarchy.AllContentTypes.ToList();
            else
                contentTypes = contentTypes
                    .SelectMany(t => ContentTypeHierarchy.ContentSubtypes.ContainsKey(t)
                                     ? ContentTypeHierarchy.ContentSubtypes[t].Append(t)
                                     : new List<Type> { t })
                    .Distinct()
                    .ToList();

            // content types must be contained within container type
            contentTypes = contentTypes.Where(ct => containerType.IsAssignableFrom(Collator.Instance.ContainerType(ct))).ToList();
            if (contentTypes.Count == 0)
                return new List<Tuple<object, Summary>>();

            var containers = ((IEnumerable<object>)ReflectionX.InvokeGenericMethod(this, "GetFilteredContainers",
                containerType, contentTypes, containerFilters.Where(f => f.Active).ToList()));

            // Summaries
            var showFields = new List<PropertyInfo>();

            Type summaryType = typeof(Summary);
            foreach (var filt in summaryFilters)
                if (summaryType.IsAssignableFrom(filt.ApplicableType))
                    summaryType = filt.ApplicableType;
                else
                    return new List<Tuple<object, Summary>>();

            var results = (List<Tuple<object, Summary>>)ReflectionX.InvokeGenericMethod(this, "FilterContainers",
                summaryType, containers, summaryFilters.Where(f => f.Active).ToList());

            var orderFilter = containerFilters.FirstOrDefault(cf => cf.Sort != 0);
            if (orderFilter == null)
                orderFilter = summaryFilters.FirstOrDefault(sf => sf.Sort != 0);
            if (orderFilter != null)
                results = orderFilter.ApplySort(results).ToList();

            return results;
        }

        /// <summary>
        /// Run a filter based on the user inputs from the Filter page including the version key values for the selected version,
        /// the list of allowed content class names, the filters and a paging spec
        /// </summary>
        /// <param name="versionFilter">list of version keys in the order they appear in VersionManager.SelectionViewModel</param>
        /// <param name="classFilter">List of content class names</param>
        /// <param name="filters">List of filters</param>
        /// <param name="pagingSpec">Specification of paging</param>
        /// <returns>A list of container x summary tuples which are the results of filtering</returns>
        public List<Tuple<object, Summary>> RunFilter(List<string> versionFilter, string[] classFilter, List<ListFilter> filters, PagingSpec pagingSpec)
        {
            if (filters == null)
                filters = new List<ListFilter>();

            var u = SecurityManager.Current.User;
            var v = VersionManager.Instance.CurrentVersion;
            var vsvms = VersionManager.Instance.SelectionViewModel(u, v);
            int vIdx = 0;

            var dict = new Dictionary<string, object>();
            foreach (var vsvm in vsvms)
            {
                object vVal = JsonConvert.DeserializeObject(versionFilter[vIdx]);
                dict.Add(vsvm.VersionKey, vVal is Int64 ? Convert.ToInt32(vVal) : vVal);
                vIdx++;
            }
            var reqVersion = new ItemVersion(dict);

            VersionManager.Instance.PushState(VersioningMode.Specific, reqVersion);

            List<Tuple<object, Summary>> pagedResult = null;
            try
            {
                var types = (classFilter ?? new string[0]).Select(c => ContentTypeHierarchy.GetAnyType(c)).ToList();

                var vm = new ItemListerViewModel();

                for (var i = 0; i < filters.Count; i++)
                    filters[i].MergeOriginal(vm.Filters[filters[i].Idx]);

                var containerFilters = filters.Where(f => (f.Active || f.Sort != 0) && !typeof(Summary).IsAssignableFrom(f.ApplicableType)).ToList();
                var summaryFilters = filters.Where(f => (f.Active || f.Sort != 0) && typeof(Summary).IsAssignableFrom(f.ApplicableType)).ToList();

                var filterResult = FilterManager.Instance.GetFilterSummaries(types, containerFilters, summaryFilters);

                pagingSpec.Total = filterResult.Count;

                pagedResult = filterResult.ApplyPaging(pagingSpec).ToList();

                //var resultView = pagedResult.Select(t => new List<string> { t.Item2.Url, t.Item2.Title }).ToList();

                //foreach (var filt in filters.Where(f => f.Show))
                //{
                //    for (int i = 0; i < resultView.Count; i++)
                //        resultView[i].Add(filt.GetShowText(pagedResult[i]));
                //}
            }
            finally
            {
                VersionManager.Instance.PopState();
            }

            return pagedResult;
        }

        /// <summary>
        /// Generate a CSV file as a string from Filter page user inputs
        /// </summary>
        /// <param name="versionFilter">list of version keys in the order they appear in VersionManager.SelectionViewModel</param>
        /// <param name="classFilter">List of content class names</param>
        /// <param name="filters">List of filters</param>
        /// <param name="pagingSpec">Specification of paging</param>
        /// <returns>A CSV as a string</returns>
        public string GenerateCsv(List<string> versionFilter, string[] classFilter, List<ListFilter> filters, PagingSpec pagingSpec)
        {
            var results = RunFilter(versionFilter, classFilter, filters, pagingSpec);

            StringBuilder sb = new StringBuilder();
            foreach (var row in results)
            {
                sb.AppendFormat("\"{0}\"", LyniconUi.ContentClassDisplayName(row.Item2.Type));
                sb.AppendFormat(",\"{0}\"", row.Item2.DisplayTitle().Replace("\"", ""));
                sb.AppendFormat(",\"{0}\"", row.Item2.Url);
                foreach (var filt in filters.Where(f => f.Show))
                {
                    sb.Append(",");

                    string vals = filt.GetShowText(row) ?? "";
                    if (vals.Contains("|"))
                    {
                        bool innerFirst = true;
                        foreach (string val in vals.Split('|'))
                        {
                            if (innerFirst)
                                innerFirst = false;
                            else
                                sb.Append(",");

                            sb.AppendFormat("\"{0}\"", val.Replace("\"", ""));
                        }
                    }
                    else
                        sb.AppendFormat("\"{0}\"", vals.Replace("\"", ""));
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
