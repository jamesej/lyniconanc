using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
//using HtmlAgilityPack;
using Lynicon.Extensibility;
using Lynicon.Utility;

namespace Lynicon.Extensions
{
    /// <summary>
    /// Manages including files or snippets of script, css or html once only
    /// </summary>
    public class IncludesManager
    {
        public const string RequestKey = "Lyn_IncludesManager";

        public static IncludesManager Instance
        {
            get
            {
                var ctx = RequestContextManager.Instance.CurrentContext;
                if (ctx.Items[RequestKey] == null)
                    ctx.Items[RequestKey] = new IncludesManager();

                // Scoped to current request
                return ctx.Items[RequestKey] as IncludesManager;
            }
            set // normally only called by ProcessIncludesAttribute
            {
                if (Instance == null)
                    RequestContextManager.Instance.CurrentContext.Items[RequestKey] = value;
            }
        }

        static IncludesManager()
        {
            // fixes a bug where Html Agility Pack always renders 'form' and 'option' as an empty tag
            // you might want to remove this behaviour if you aren't using XHTML doctype, it was apparently
            // added to deal with overlapping tags which are valid HTML.
            //HtmlNode.ElementsFlags.Remove("form");
            //HtmlNode.ElementsFlags.Remove("option");

            // When process includes attribute fires global event, catch it and process it with the current
            // request's IncludeManager instance
            //EventHub.Instance.RegisterEventProcessor("PostProcess.Html",
            //    ehd => (Instance == null) ? ehd.Data : Instance.ProcessDocument(ehd),
            //    "IncludesManager");
        }

        public IncludesManager()
        {
            Scripts = new List<IncludeEntry>();
            Csses = new List<IncludeEntry>();
            Htmls = new List<IncludeEntry>();
            Styles = new List<IncludeEntry>();
        }

        /// <summary>
        /// List of include entries representing scripts
        /// </summary>
        public List<IncludeEntry> Scripts { get; set; }
        /// <summary>
        /// List of include entries representing css
        /// </summary>
        public List<IncludeEntry> Csses  { get; set; }
        /// <summary>
        /// List of include entries representing html
        /// </summary>
        public List<IncludeEntry> Htmls { get; set; }
        /// <summary>
        /// List of include entries representing local styles
        /// </summary>
        public List<IncludeEntry> Styles { get; set; }

        #region Html Processing

        //private HtmlNode MakeNode(HtmlDocument doc, string tag, string mainAttr, string mainValue, Dictionary<string, string> createAttributes)
        //{
        //    // can't create a new node for a script where the value is its id
        //    if (mainAttr == "src" && Regex.IsMatch(mainValue.ToLower(), "^[a-z][a-z0-9_\\-]*$"))
        //        return null;

        //    HtmlNode newNode = doc.CreateElement(tag);
        //    if (mainValue.StartsWith("javascript:"))
        //        newNode.AppendChild(doc.CreateTextNode(mainValue.After("javascript:")));
        //    else
        //        newNode.Attributes.Add(doc.CreateAttribute(mainAttr, mainValue));
        //    foreach (KeyValuePair<string, string> kvp in createAttributes)
        //        newNode.Attributes.Add(doc.CreateAttribute(kvp.Key, kvp.Value));
        //    return newNode;
        //}

        /// <summary>
        /// Update the given html document with a specific kind of includes from a list
        /// </summary>
        /// <param name="doc">The HTML document</param>
        /// <param name="tag">The tag name which is being updated</param>
        /// <param name="attr">The attribute of the tag which is set with the url of the include</param>
        /// <param name="includes">The list of include specifications</param>
        /// <param name="createAttributes">Attributes to add to created HTML nodes</param>
        //public void UpdateIncludes(HtmlDocument doc, string tag, string attr, List<IncludeEntry> includes, Dictionary<string, string> createAttributes)
        //{
        //    if (includes == null || includes.Count == 0)
        //        return;

        //    List<HtmlNode> existingIncludes = doc.DocumentNode
        //        .Descendants(tag)
        //        .Where(n => n.GetAttributeValue(attr, "") != "")
        //        .ToList();
        //    int c = existingIncludes.Count;

        //    // Remove duplicates leaving later item in place
        //    for (int i = 1; i < c; i++)
        //        for (int j = 0; j < i; j++)
        //            if (existingIncludes[i].GetAttributeValue(attr, "") == existingIncludes[j].GetAttributeValue(attr, ""))
        //            {
        //                existingIncludes.RemoveAt(j);
        //                i--; c--;
        //                break;
        //            }

        //    List<string> newIncludes = CreateIncludeList(
        //        existingIncludes
        //            .Select(n => new IncludeEntry
        //                    {
        //                        Include = n.GetAttributeValue(attr, ""),
        //                        Id = n.GetAttributeValue("id", "")
        //                    }).ToList(),
        //        includes);
        //    int pos = 0;
        //    foreach (HtmlNode existingIncl in existingIncludes)
        //    {
        //        while (pos < newIncludes.Count && newIncludes[pos] != existingIncl.GetAttributeValue(attr, ""))
        //        {
        //            HtmlNode newNode = MakeNode(doc, tag, attr, newIncludes[pos], createAttributes);
        //            if (newNode != null)
        //                existingIncl.ParentNode.InsertBefore(newNode, existingIncl);
        //            pos++;
        //        }
        //        pos++;
        //    }
        //    HtmlNode incl = existingIncludes.LastOrDefault();
        //    if (incl != null)
        //        while (pos < newIncludes.Count)
        //        {
        //            HtmlNode newNode = MakeNode(doc, tag, attr, newIncludes[pos], createAttributes);
        //            if (newNode != null)
        //                incl.ParentNode.InsertAfter(newNode, incl);
        //            pos++;
        //        }
        //    else
        //    {
        //        HtmlNode head = doc.DocumentNode.Element("html").Element("head");
        //        if (head != null)
        //        {
        //            pos = newIncludes.Count - 1;
        //            while (pos >= 0)
        //            {
        //                HtmlNode newNode = MakeNode(doc, tag, attr, newIncludes[pos], createAttributes);
        //                if (newNode != null)
        //                    head.PrependChild(newNode);
        //                pos--;
        //            }
        //        }
        //    }

        //}

        /// <summary>
        /// Insert HTML snippets into an HTML document
        /// </summary>
        /// <param name="doc">HTML document</param>
        /// <param name="htmls">HTML snippet include entries</param>
        //public void InsertHtmls(HtmlDocument doc, List<IncludeEntry> htmls)
        //{
        //    HtmlNode body = doc.DocumentNode.Element("html").Element("body");
        //    foreach (IncludeEntry html in htmls)
        //    {
        //        HtmlNode insHtml = HtmlNode.CreateNode("<div>" + html.Include + "</div>");
        //        if (insHtml.ChildNodes.Count == 1)
        //            insHtml = insHtml.FirstChild;
        //        insHtml.Attributes.Add("id", html.Id);
        //        body.AppendChild(insHtml);
        //    }
        //}

        /// <summary>
        /// Insert local styles into an HTML document
        /// </summary>
        /// <param name="doc">HTML document</param>
        /// <param name="styles">Local style include entries</param>
        //public void InsertStyles(HtmlDocument doc, List<IncludeEntry> styles)
        //{
        //    if (styles == null || styles.Count == 0)
        //        return;

        //    StringBuilder sb = new StringBuilder();
        //    foreach (var style in styles)
        //    {
        //        sb.AppendLine(style.Include);
        //    }
        //    doc.DocumentNode.Element("html").Element("head")
        //        .AppendChild(HtmlNode.CreateNode("<style>" + sb.ToString() + "</style>"));
        //}

        /// <summary>
        /// Build a sorted list of includes from existing includes in the document and requested/registered includes
        /// </summary>
        /// <param name="existing">Existing includes in the document</param>
        /// <param name="requested">Registered includes to add</param>
        /// <returns>List of include files or script, css or html snippets in order to insert in document</returns>
        public List<string> CreateIncludeList(List<IncludeEntry> existing, List<IncludeEntry> requested)
        {
            var primaries = requested
                .Where(ie => ie.Dependencies == null)
                .Select(ie => ie.Include)
                .Concat(existing.Select(ie => ie.Include))
                .ToList();

            var inclDict = existing
                .Concat(requested)
                .Where(ie => !string.IsNullOrEmpty(ie.Id))
                .ToDictionary(ie => ie.Id, ie => ie.Include);

            // creates a list of lists where in each list each item depends on the ones before it
            List<List<string>> depLists = requested
                .Where(ie => !primaries.Contains(ie.Include))
                .Select(ie => ie.Dependencies
                        .Select(d => inclDict.ContainsKey(d) ? inclDict[d] : d)
                        .Append(ie.Include).ToList())
                .ToList();

            // if there are no dependencies, just return the list of primaries
            if (depLists.Count == 0)
                return primaries.ToList();

            // distinct items in lists
            List<string> items = depLists.SelectMany(list => list).Concat(primaries).Distinct().ToList();

            // build graph of ordering information
            ArrayGraph<string, bool> orderingGraph = new ArrayGraph<string, bool>(items) { Unidirectional = true };

            var iPrimaries = primaries.Select(p => orderingGraph.NodeIndex(p)).ToArray();

            // ensure primaries retain same order
            for (int i = 1; i < iPrimaries.Length; i++)
                for (int j = 0; j < i; j++)
                    orderingGraph[iPrimaries[j], iPrimaries[i]] = true;

            var iDepLists = depLists.Select(dl => dl.Select(dli => orderingGraph.NodeIndex(dli)).ToArray()).ToArray();
            // add dependency lists
            for (int i = 0; i < iDepLists.Length; i++)
                for (int j = 1; j < iDepLists[i].Length; j++)
                    for (int k = j - 1; k >= 0; k--)
                        orderingGraph[iDepLists[i][k], iDepLists[i][j]] = true;

            // use it to order includes
            List<string> result = items
                .PartialOrderBy(s => s, orderingGraph)
                .ToList();

            return result;
        }

        /// <summary>
        /// Global event processor for processing output html
        /// </summary>
        /// <param name="ehd">EventHubData for output html</param>
        /// <returns>Event data output, the HtmlDocument after processing</returns>
        //public object ProcessDocument(EventHubData ehd)
        //{
        //    var doc = (HtmlDocument)ehd.Data;
        //    DateTime st = DateTime.Now;
        //    if (Csses != null)
        //        UpdateIncludes(doc, "link", "href", Csses,
        //            new Dictionary<string, string> { { "rel", "stylesheet" }, { "type", "text/css" } });

        //    if (Scripts != null)
        //        UpdateIncludes(doc, "script", "src", Scripts,
        //            new Dictionary<string, string> { { "type", "text/javascript" } });

        //    if (Htmls != null)
        //        InsertHtmls(doc, Htmls);

        //    if (Styles != null)
        //        InsertStyles(doc, Styles);

        //    return doc;
        //}

        #endregion
    }
}
