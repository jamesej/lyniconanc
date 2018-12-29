using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Utility;
using Microsoft.AspNetCore.Routing;

namespace Lynicon.Models
{
    /// <summary>
    /// A class which specifies how data is paged, links to OData style query string values
    /// </summary>
    public class PagingSpec
    {
        public static List<string> PagingKeys = new List<string> { "$skip", "$take", "$top", "$orderby" };

        /// <summary>
        /// Sets up a PagingSpec from OData query string values in a RouteValueDictionary for a request
        /// </summary>
        /// <param name="rvd">RouteValueDictionary for a request</param>
        /// <returns>the resulting PagingSpec</returns>
        public static PagingSpec Create(RouteValueDictionary rvd)
        {
            var spec = new PagingSpec();
            int skip = 0;
            if (rvd.ContainsKey("$skip"))
                int.TryParse((string)rvd["$skip"], out skip);
            int take = int.MaxValue;
            if (rvd.ContainsKey("$take"))
                int.TryParse((string)rvd["$take"], out take);
            else if (rvd.ContainsKey("$top"))
                int.TryParse((string)rvd["$top"], out take);
            if (rvd.ContainsKey("$orderby"))
                spec.Sort = (string)rvd["$orderby"];
            if (skip != 0 || take != 0 || spec.Sort != null)
            {
                spec.Skip = skip;
                spec.Take = take;
                return spec;
            }
            else
                return null;
        }
        /// <summary>
        /// Sets up a paging spec from the query string values as a NameValueCollection
        /// </summary>
        /// <param name="nvc">Query string values as a NameValueCollection</param>
        /// <returns>The resulting PagingSpec</returns>
        public static PagingSpec Create(NameValueCollection nvc)
        {
            return Create(new RouteValueDictionary(nvc.ToKeyValues().ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value, kvp => { })));
        }

        /// <summary>
        /// How many items to skip in the data list
        /// </summary>
        public int Skip { get; set; }
        /// <summary>
        /// How many items to take from the data list after skipping
        /// </summary>
        public int Take { get; set; }

        int total = 0;
        /// <summary>
        /// The total number of items in the data list
        /// </summary>
        public int Total
        {
            get { return Math.Max(total, Skip); } // we want to be able to control return from a skip beyond the end of the items
            set { total = value; }
        }
        /// <summary>
        /// The field by which to sort the data list
        /// </summary>
        public string Sort { get; set; }
        /// <summary>
        /// Name of a client JS function to reload the data list with new paging
        /// </summary>
        public string ClientReload { get; set; }

        /// <summary>
        /// The page number of data (page length being the Take property)
        /// </summary>
        public int Page
        {
            get
            {
                if (Take == 0)
                    return 0;
                else
                    return Skip / Take;
            }
        }

        /// <summary>
        /// The total number of pages of data (page length being the Take property)
        /// </summary>
        public int TotalPages
        {
            get
            {
                if (Take == 0)
                    return 1;
                else
                    return (int)Math.Ceiling((double)Total / Take);
            }
        }

        /// <summary>
        /// Whether there are enough items to need a pager
        /// </summary>
        public bool NeedsPager
        {
            get
            {
                return Take <= Total;
            }
        }

        /// <summary>
        /// Get the url to link to a page of data given the current url and the page number
        /// </summary>
        /// <param name="currentUrl">Current url</param>
        /// <param name="page">Page number</param>
        /// <returns>Resulting url</returns>
        public string GetUrl(string currentUrl, int page)
        {
            int skip = page * Take;
            if (skip < 0) skip = 0;
            if (skip >= Total) skip = (int)Math.Floor((double)(Total / Take)) * Take;

            var url = UrlX.EnsureQueryKeyValue(currentUrl, "$skip", skip.ToString());
            if (!string.IsNullOrEmpty(Sort))
                url = UrlX.EnsureQueryKeyValue(url, "$orderBy", Sort);

            return url;
        }

        /// <summary>
        /// Page range to show in the page given the number of pages it should show at maximum
        /// </summary>
        /// <param name="width">maximum number of pages to list</param>
        /// <returns>List of page numbers to show as page links in the pager</returns>
        public IEnumerable<int> PageRange(int width)
        {
            if (width > TotalPages)
                width = TotalPages;
            int start = Page - width / 2;
            if (start + width > TotalPages)
                start = TotalPages - width;
            if (start < 0)
                start = 0;
            yield return 0;
            if (start - width > 0)
                yield return start - width;
            for (int i = start + 1; i < start + width - 1; i++)
                yield return i;
            if (start + 2 * width < TotalPages)
                yield return start + 2 * width - 1;
            if (TotalPages > 1)
                yield return TotalPages - 1;
        }

        /// <summary>
        /// If the page at the given index in the paging links should be a spacer link
        /// </summary>
        /// <param name="width">The width of the paging links</param>
        /// <param name="p">The index of the paging link to show</param>
        /// <returns>Whether the paging link is a spacer</returns>
        public bool IsSpacerPage(int width, int p)
        {
            return ((p < Page - width / 2) || (p > Page + width / 2 + 1));
        }

        /// <summary>
        /// If the page at the given index in the paging links is an end link
        /// </summary>
        /// <param name="width">The width of the paging link</param>
        /// <param name="p">The index of the paging link to show</param>
        /// <returns>Whether the paging link is an end page</returns>
        public bool IsEndPage(int width, int p)
        {
            return IsSpacerPage(width, p) && (p == 0 || p == TotalPages - 1);
        }
    }
}
