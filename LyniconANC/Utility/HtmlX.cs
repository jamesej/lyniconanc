using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using HtmlAgilityPack;

namespace Lynicon.Utility
{
    public static class HtmlX
    {
        /// <summary>
        /// Output an exception as sensible HTML
        /// </summary>
        /// <param name="ex">The exception</param>
        /// <returns>The markup for the exception</returns>
        public static string ToHtml(this Exception ex)
        {
            StringBuilder html = new StringBuilder();
            html.AppendFormat("<div class='ex-message'>{0}</div>", ex.Message);
            html.AppendFormat("<div class='ex-stack-trace'>{0}</div>", (ex.StackTrace ?? "").Replace(Environment.NewLine, "<br/>"));
            if (ex.InnerException != null)
                html.Append(ex.InnerException.ToHtml());
            return html.ToString();
        }
        /// <summary>
        /// Cleans up an HTML string and limits HTML to whitelist
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string MinimalHtml(string html, bool allowLinks)
        {
            var acceptedTags = new List<string> { "b", "i", "strong", "em", "p", "sub", "sup", "br", "div", "h1", "h2", "h3", "h4", "h5" };
            if (allowLinks)
                acceptedTags.Add("a");
            return TagProcess(html, acceptedTags);
        }

        /// <summary>
        /// Turn string with new paragraph indicated by \n into HTML paragraphs
        /// </summary>
        /// <param name="plain">string with \n paragraphs</param>
        /// <returns>string with p tags for paragraphs</returns>
        public static string Htmlify(string plain)
        {
            plain = plain.Trim();
            return "<p>" + plain.Replace("\n", "</p><p>").Replace("\r", "</p><p>").Replace("<p></p>", "") + "</p>";
        }

        /// <summary>
        /// Cleans up an HTML string by removing elements
        /// on the blacklist and all elements that start
        /// with onXXX .
        /// </summary>
        /// <param name="html">The HTML</param>
        /// <returns>The cleaned HTML</returns>
        public static string TagProcess(string html, List<string> acceptedTags)
        {
            var doc = new HtmlDocument();

            doc.OptionWriteEmptyNodes = true; // self-close tags
            doc.LoadHtml(html);

            var simplifyTags = new Action<HtmlNode>(n =>
            {
                if (!acceptedTags.Contains(n.Name))
                {
                    n.ParentNode.ReplaceChild(n.OwnerDocument.CreateTextNode(n.InnerText), n);
                }
                else
                {
                    if (n.Name == "b")
                        n.Name = "strong";
                    else if (n.Name == "i")
                        n.Name = "em";
                    else if (n.Name == "u")
                        n.Name = "em";
                    else if (new List<string> { "div", "h1", "h2", "h3", "h4", "h5" }.Contains(n.Name))
                        n.Name = "p";
                    else if (n.Name == "p")
                    {
                        if (string.IsNullOrWhiteSpace(n.InnerText.Replace("&nbsp;", "")))
                            n.Remove();
                    }
                    Clean(n);
                }
            });
            var brFix = new Action<HtmlNode>(n =>
            {
                if (n.Name == "br")
                {
                    HtmlNode nPrev = n.PreviousSibling;
                    bool hasWS = n.NextSibling == null || NodeIsEmpty(n.NextSibling) || n.PreviousSibling == null || NodeIsEmpty(n.PreviousSibling);
                    if (!hasWS)
                        n.ParentNode.InsertAfter(n.OwnerDocument.CreateTextNode(" "), n);

                    n.Remove();
                }
            });

            NodeProcess(doc.DocumentNode, simplifyTags);
            PutTextInParas(doc, null);
            NodeProcess(doc.DocumentNode, brFix);
            KillBlankParas(doc);

            //return doc.DocumentNode.WriteTo();

            return Output(doc.DocumentNode);
        }

        /// <summary>
        /// Test whether an HtmlNode is empty
        /// </summary>
        /// <param name="node">HtmlNode</param>
        /// <returns>True if empty</returns>
        public static bool NodeIsEmpty(HtmlNode node)
        {
            return node.NodeType == HtmlNodeType.Text && string.IsNullOrWhiteSpace(node.InnerText.Replace("&nbsp;", ""));
        }

        /// <summary>
        /// Convert an HtmlNode into a string
        /// </summary>
        /// <param name="node">HtmlNode</param>
        /// <returns>string of the HTML</returns>
        public static string Output(HtmlNode node)
        {
            string output = null;

            using (StringWriter sw = new StringWriter())
            {
                node.WriteTo(sw);
                output = sw.ToString();

                // strip off XML doc header
                if (!string.IsNullOrEmpty(output))
                {
                    int at = output.IndexOf("?>");
                    if (at >= 0)
                        output = output.Substring(at + 2);
                }
            }

            return output;
        }

        /// <summary>
        /// Takes an HTML structure which can contain <br>s, and convert the <br>s into </p><p>
        /// in a sensible way
        /// </summary>
        /// <param name="doc">Html document</param>
        /// <param name="node">The HtmlNode to convert</param>
        public static void PutTextInParas(HtmlDocument doc, HtmlNode node)
        {
            HtmlNode para = doc.CreateElement("p");
            HtmlNode parentNode = node ?? doc.DocumentNode;
            HtmlNode topNode = parentNode.FirstChild;
            while (topNode != null)
            {
                HtmlNode nextNode = topNode.NextSibling;
                bool isParaBreak = false;
                bool removeAfterInsertPara = false;
                if (topNode.Name == "p")
                {
                    isParaBreak = true;
                }
                else if (topNode.Name == "br")   // convert double br to para break
                {
                    var findBrNode = topNode.NextSibling;
                    while (findBrNode != null && NodeIsEmpty(findBrNode))
                        findBrNode = findBrNode.NextSibling;
                    if (findBrNode != null && findBrNode.Name == "br")
                    {
                        isParaBreak = true;
                        HtmlNode topNode1;
                        while (topNode != findBrNode) // remove nodes before second br
                        {
                            topNode1 = topNode.NextSibling;
                            topNode.Remove();
                            topNode = topNode1;
                        }
                        nextNode = topNode.NextSibling; // top node is at second br
                        removeAfterInsertPara = true;
                    }
                }
                if (isParaBreak)
                {
                    if (para.HasChildNodes)
                    {
                        parentNode.InsertBefore(para, topNode);
                        para = doc.CreateElement("p");
                        if (removeAfterInsertPara)
                            topNode.Remove();
                    }

                    if (topNode.Name == "p" && node == null) // recurse into top level p to see if it has br's to split it
                    {
                        PutTextInParas(doc, topNode);
                        HtmlNode childNode = topNode.FirstChild;
                        while (childNode != null)
                        {
                            HtmlNode nextChildNode = childNode.NextSibling;
                            childNode.Remove();
                            parentNode.InsertBefore(childNode, topNode);
                            childNode = nextChildNode;
                        }
                        topNode.Remove();
                    }
                }
                else
                {
                    topNode.Remove();
                    para.AppendChild(topNode);
                }
                topNode = nextNode;
            }
            if (para.HasChildNodes)
                parentNode.AppendChild(para);
        }

        /// <summary>
        /// Remove empty paragraphs from an HtmlDocument
        /// </summary>
        /// <param name="doc">HtmlDocument to remove empty paragraphs from</param>
        public static void KillBlankParas(HtmlDocument doc)
        {
            HtmlNode topNode = doc.DocumentNode.FirstChild;
            while (topNode != null)
            {
                HtmlNode nextNode = topNode.NextSibling;
                if (topNode.Name == "p" && string.IsNullOrWhiteSpace(topNode.InnerText.Replace("&nbsp;", "")))
                {
                    topNode.Remove();
                }
                topNode = nextNode;
            }
        }

        /// <summary>
        /// Remove CSS local styles, classes, embedded script links, event attributes with JS handlers.  Fix
        /// hrefs starting '~'
        /// </summary>
        /// <param name="node">The HtmlNode to clean</param>
        public static void Clean(HtmlNode node)
        {
            // remove CSS Expressions and embedded script links
            if (node.Name == "style")
            {
                if (string.IsNullOrEmpty(node.InnerText))
                {
                    if (node.InnerHtml.Contains("expression") || node.InnerHtml.Contains("javascript:"))
                        node.ParentNode.RemoveChild(node);
                }
            }

            // remove script attributes
            if (node.HasAttributes)
            {
                for (int i = node.Attributes.Count - 1; i >= 0; i--)
                {
                    HtmlAttribute currentAttribute = node.Attributes[i];

                    var attr = currentAttribute.Name.ToLower();
                    var val = currentAttribute.Value.ToLower();

                    // remove event handlers
                    if (attr.StartsWith("on"))
                        node.Attributes.Remove(currentAttribute);

                    // remove script links
                    else if (
                        //(attr == "href" || attr== "src" || attr == "dynsrc" || attr == "lowsrc") &&
                             val != null &&
                             val.Contains("javascript:"))
                        node.Attributes.Remove(currentAttribute);

                    // Remove CSS Styles and classes
                    else if ((attr == "style" || attr == "class") &&
                             val != null)
                        node.Attributes.Remove(currentAttribute);

                    // Fix hrefs
                    else if (attr == "href" && val != null && val.StartsWith("~"))
                        currentAttribute.Value = currentAttribute.Value.Substring(1);
                }
            }
        }

        /// <summary>
        /// Run a process recursively on an HtmlNode and all those it contains
        /// </summary>
        /// <param name="node">The top level HtmlNode</param>
        /// <param name="process">Action on an HtmlNode</param>
        public static void NodeProcess(HtmlNode node, Action<HtmlNode> process)
        {
            if (node.NodeType == HtmlNodeType.Element)
            {
                process(node);
            }
            else if (node.NodeType == HtmlNodeType.Comment)
            {
                node.Remove();
            }

            // Look through child nodes recursively
            if (node.HasChildNodes)
            {
                for (int i = node.ChildNodes.Count - 1; i >= 0; i--)
                {
                    if (i >= node.ChildNodes.Count)
                        i = node.ChildNodes.Count - 1;
                    NodeProcess(node.ChildNodes[i], process);
                }
            }
        }
    }
}
