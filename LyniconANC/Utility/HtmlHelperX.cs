using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.ComponentModel;
using Newtonsoft.Json;
using Lynicon.Extensions;
using Lynicon.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lynicon.Utility
{
    /// <summary>
    /// Used for indicating an address element should have no value
    /// </summary>
    public enum AddressElement
    {
        None
    }

    /// <summary>
    /// @Html utilities to perform functions related to Lynicon
    /// </summary>
    public static class IHtmlHelperX
    {
        public static readonly int MaxDropDownItems = 80;

        #region Include Management

        private static bool RegisterInclude(Func<List<IncludeEntry>> getList, string id, string include, List<string> dependencies)
        {
            if (IncludesManager.Instance == null)
                throw new Exception("Trying to register include: " + id + " on page with no ProcessIncludesAttribute");
            var list = getList();
            if (list.Any(ie => ie.Id == id))
                return false;
            else
            {
                list.Add(new IncludeEntry { Id = id, Include = include, Dependencies = dependencies });
                return true;
            }
        }

        /// <summary>
        /// Register a script to be included in the page once
        /// </summary>
        /// <param name="html">Html helper</param>
        /// <param name="script">The url of the script, or 'javascript:' followed by the script code itself</param>
        /// <returns>Empty string</returns>
        public static string RegisterScript(this IHtmlHelper html, string script)
        {
            return RegisterScript(html, script, script, new List<string>());
        }
        /// <summary>
        /// Register a script to be included in the page once, including dependencies
        /// </summary>
        /// <param name="html">Html helper</param>
        /// <param name="id">An id for the script to use in dependency lists</param>
        /// <param name="script">The url of the script, or 'javascript:' followed by the script code itself</param>
        /// <param name="dependencies">List of ids of scripts this depends on, so must come before it in the output file</param>
        /// <returns>Empty string</returns>
        public static string RegisterScript(this IHtmlHelper html, string id, string script, List<string> dependencies)
        {
            RegisterInclude(() => IncludesManager.Instance.Scripts, id, script, dependencies);
            return "";
        }

        /// <summary>
        /// Register a css file to be included in the page once
        /// </summary>
        /// <param name="html">Html helper</param>
        /// <param name="css">The url of the css file</param>
        /// <returns>Empty string</returns>
        public static string RegisterCss(this IHtmlHelper html, string css)
        {
            RegisterInclude(() => IncludesManager.Instance.Csses, css, css, new List<string>());
            return "";
        }

        /// <summary>
        /// Register an HTML block to be included in the page once
        /// </summary>
        /// <param name="html">Html helper</param>
        /// <param name="id">An id for the HTML block</param>
        /// <param name="htmlBlock">The HTML block as an MvcHtmlString</param>
        /// <returns>Empty string</returns>
        public static string RegisterHtmlBlock(this IHtmlHelper html, string id, HtmlString htmlBlock)
        {
            RegisterInclude(() => IncludesManager.Instance.Htmls, id, htmlBlock.ToString(), new List<string>());
            return "";
        }
        /// <summary>
        /// Register an HTML block to be included in the page once
        /// </summary>
        /// <param name="html">Html helper</param>
        /// <param name="id">An id for the HTML block</param>
        /// <param name="htmlBlock">The HTML block as a string</param>
        /// <returns>Empty string</returns>
        public static string RegisterHtmlBlock(this IHtmlHelper html, string id, string htmlBlock)
        {
            RegisterInclude(() => IncludesManager.Instance.Htmls, id, htmlBlock, new List<string>());
            return "";
        }

        /// <summary>
        /// Test whether an HTML block was already registered by id
        /// </summary>
        /// <param name="html">Html helper</param>
        /// <param name="id">An id to check for</param>
        /// <returns>Whether the id was already registered</returns>
        public static bool HtmlBlockIsRegistered(this IHtmlHelper html, string id)
        {
            return IncludesManager.Instance.Htmls.Any(incl => incl.Id == id);
        }

        /// <summary>
        /// Test whether a script was already registered by id
        /// </summary>
        /// <param name="html">Html helper</param>
        /// <param name="id">An id to check for</param>
        /// <returns>Whether the id was already registered</returns>
        public static bool ScriptIsRegistered(this IHtmlHelper html, string id)
        {
            return IncludesManager.Instance.Scripts.Any(incl => incl.Id == id);
        }

        /// <summary>
        /// Test whether a css file was already registered by id
        /// </summary>
        /// <param name="html">Html helper</param>
        /// <param name="id">An id to check for</param>
        /// <returns>Whether the id was already registered</returns>
        public static bool CssIsRegistered(this IHtmlHelper html, string id)
        {
            return IncludesManager.Instance.Csses.Any(incl => incl.Id == id);
        }
        /// <summary>
        /// Test whether a local style block was already registered by id
        /// </summary>
        /// <param name="html">Html helper</param>
        /// <param name="id">An id to check for</param>
        /// <returns>Whether the id was already registered</returns>
        public static bool LocalStyleIsRegistered(this IHtmlHelper html, string id)
        {
            return IncludesManager.Instance.Styles.Any(incl => incl.Id == id);
        }

        /// <summary>
        /// Register some CSS lines to be included in a local style block
        /// </summary>
        /// <param name="html">Html helper</param>
        /// <param name="id">An id to check for</param>
        /// <param name="style">CSS lines to include in a local style block</param>
        /// <returns>Empty string</returns>
        public static HtmlString RegisterLocalStyles(this IHtmlHelper html, string id, string style)
        {
            if (RegisterInclude(() => IncludesManager.Instance.Styles, id, style, new List<string>()))
            {
                var sb = new StringBuilder();
                sb.AppendLine("<style type='text/css'>");
                sb.AppendLine(style);
                sb.AppendLine("</style>");
                return new HtmlString(sb.ToString());
            }
            return new HtmlString("");
        }

        public static HtmlString InsertIncludesHeader(this IHtmlHelper html)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var incl in IncludesManager.Instance.Csses)
            {
                sb.AppendLine($"<link rel='stylesheet' type='text/css' href='{incl.Include}' />");
            }
            if (IncludesManager.Instance.Styles.Count > 0)
            {
                sb.AppendLine("<style type='text/css'>");
                foreach (var incl in IncludesManager.Instance.Styles)
                {
                    sb.AppendLine(incl.Include);
                }
                sb.AppendLine("</style>");
            }

            return new HtmlString(sb.ToString());
        }

        public static HtmlString InsertIncludesBody(this IHtmlHelper html)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var incl in IncludesManager.Instance.Htmls)
            {
                sb.AppendLine(incl.Include);
            }

            foreach (var incl in IncludesManager.Instance.Scripts)
            {
                if (incl.Include.StartsWith("javascript:"))
                {
                    sb.AppendLine("<script type='text/javascript'>");
                    sb.AppendLine(incl.Include.Substring("javascript:".Length));
                    sb.AppendLine("</script>");
                }
                else if (string.IsNullOrEmpty(incl.Id))
                    sb.AppendLine($"<script type='text/javascript' src='{incl.Include}'></script>");
                else
                    sb.AppendLine($"<script id='{incl.Id}' type='text/javascript' src='{incl.Include}'></script>"); 
            }

            return new HtmlString(sb.ToString());
        }

        #endregion

        /// <summary>
        /// Pull a number of values from ViewData and put these into a string of attributes to include in markup
        /// </summary>
        /// <param name="helper">Html helper</param>
        /// <param name="attributeNames">List of attributes, space separated, whose values are in ViewData with the same key</param>
        /// <returns>Markup of attribute declarations</returns>
        public static HtmlString AttributesFor(this IHtmlHelper helper, string attributeNames)
        {
            string attrs = attributeNames.Split(' ')
                .Where(n => helper.ViewData.ContainsKey(n))
                .Select(n => n + "=\"" + helper.ViewData[n].ToString() + "\"")
                .Join(" ");
            return new HtmlString(attrs);
        }

        /// <summary>
        /// A DropDownListFor case which works by showing the possible values for an enum type
        /// </summary>
        /// <typeparam name="TModel">Type of the model</typeparam>
        /// <typeparam name="TProperty">The type of the model's property this is an editor for</typeparam>
        /// <param name="html">Html helper</param>
        /// <param name="expression">selector for the property to edit</param>
        /// <param name="enumType">the enum type from which to get the possible values</param>
        /// <returns>Markup for the dropdown list editor</returns>
        public static IHtmlContent DropDownListFor<TModel, TProperty>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TProperty>> expression, Type enumType)
        {
            List<SelectListItem> list = new List<SelectListItem>();
            Dictionary<string, string> enumItems = enumType.GetDescription();
            foreach (KeyValuePair<string, string> pair in enumItems)
                list.Add(new SelectListItem() { Value = pair.Key, Text = pair.Value });
            return html.DropDownListFor(expression, list);
        }

        public static IHtmlContent EditorForItem<TModel>(this IHtmlHelper<TModel> html, object item, string templateName, string fieldName, object additionalViewData)
        {
            var res = (IHtmlContent)typeof(IHtmlHelperX)
                .GetMethod("EditorForItemInner", BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(typeof(TModel), item.GetType())
                .Invoke(null, new object[] { html, item, templateName, fieldName, additionalViewData });
            return res;
        }

        private static IHtmlContent EditorForItemInner<TModel, TProp>(this IHtmlHelper<TModel> html, TProp item, string templateName, string htmlFieldName, object additionalViewData)
        {
            //var parm = Expression.Parameter(typeof(TModel));
            //var body = Expression.Constant(item);
            //var body = Expression.
            //var selector = Expression.Lambda<Func<TModel, TProp>>(body, parm);
            return html.EditorFor(m => item, templateName, htmlFieldName, additionalViewData);
        }

        /// <summary>
        /// return the items of enum paired with its description.
        /// </summary>
        /// <param name="enumeration">enumeration type to be processed.</param>
        /// <returns></returns>
        public static Dictionary<string, string> GetDescription(this Type enumeration)
        {
            if (!enumeration.IsEnum)
            {
                throw new ArgumentException("passed type must be of Enum type", "enumerationValue");
            }

            Dictionary<string, string> descriptions = new Dictionary<string, string>();
            var members = enumeration.GetMembers().Where(m => m.MemberType == MemberTypes.Field);

            foreach (MemberInfo member in members)
            {
                var attrs = member.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attrs.Count() != 0)
                    descriptions.Add(member.Name, ((DescriptionAttribute)attrs[0]).Description);
            }
            return descriptions;
        }

        /// <summary>
        /// Add script to assign a value to a global JS variable to make a value available in script
        /// </summary>
        /// <param name="html">Html helper</param>
        /// <param name="varName">JS name of the variable</param>
        /// <param name="value">Value to which to set the variable (can be arbitrarily complex)</param>
        /// <returns>Script markup for setting the variable</returns>
        public static HtmlString ClientScriptAssign(this IHtmlHelper html, string varName, object value)
        {
            string jsonValue = JsonConvert.SerializeObject(value);
            string output = string.Format("<script type='text/javascript'>var {0} = {1};</script>", varName, jsonValue);
            return new HtmlString(output);
        }

        /* TMP CONTENTACTION
        /// <summary>
        /// Include the output of a content route in the page
        /// </summary>
        /// <param name="html">Html helper</param>
        /// <param name="action">Name of the action method</param>
        /// <param name="controller">Name of the controller</param>
        /// <returns>The output of content route action method</returns>
        public static HtmlString ContentAction(this IHtmlHelper html, string action, string controller)
        {
            return html.ContentAction(action, controller, new RouteValueDictionary());
        }
        /// <summary>
        /// Include the output of a content route in the page
        /// </summary>
        /// <param name="html">Html helper</param>
        /// <param name="action">Name of the action method</param>
        /// <param name="controller">Name of the controller</param>
        /// <param name="rvs">Anonymous object with values to add into route values which go into action method</param>
        /// <returns>The output of content route action method</returns>
        public static HtmlString ContentAction(this IHtmlHelper html, string action, string controller, object rvs)
        {
            return ContentAction(html, action, controller, new RouteValueDictionary(rvs));
        }
        /// <summary>
        /// Include the output of a content route in the page
        /// </summary>
        /// <param name="html">Html helper</param>
        /// <param name="action">Name of the action method</param>
        /// <param name="controller">Name of the controller</param>
        /// <param name="rvs">RouteValueDictionary with values which go into the RouteData for the action method</param>
        /// <returns>The output of content route action method</returns>
        public static HtmlString ContentAction(this IHtmlHelper html, string action, string controller, RouteValueDictionary rvs)
        {
            var urls = new UrlHelper(html.ViewContext.RequestContext);
            RouteValueDictionary localRvs = new RouteValueDictionary(rvs);
            var url = urls.Action(action, controller, localRvs);
            html.ViewContext.RouteData.Values["data"] = html.ViewContext.RouteData.DataTokens["lyniconVpData"];
            return html.Action(action, controller, localRvs);
        }
        */
        /// <summary>
        /// Include markup for a list pager based on the PagingSpec stored in the DataToken whose key is @Paging
        /// </summary>
        /// <param name="html">Html helper</param>
        /// <returns>Markup for list pager</returns>
        public static IHtmlContent ListPager(this IHtmlHelper html)
        {
            return html.ListPager(null);
        }
        /// <summary>
        /// Include markup for a list pager based on the PagingSpec stored in the DataToken whose key is @Paging
        /// </summary>
        /// <param name="html">Html helper</param>
        /// <param name="clientReload">Client function name to call to reload the list to which the pager is attached</param>
        /// <returns>Markup for list pager</returns>
        public static IHtmlContent ListPager(this IHtmlHelper html, string clientReload)
        {
            return html.ListPager(clientReload, "~/Areas/Lynicon/Views/Shared/PagingSpec.cshtml");
        }
        /// <summary>
        /// Include markup for a list pager based on the PagingSpec stored in the DataToken whose key is @Paging
        /// </summary>
        /// <param name="html">Html helper</param>
        /// <param name="clientReload">Client function name to call to reload the list to which the pager is attached</param>
        /// <param name="viewName">View name which which to render the pager</param>
        /// <returns>Markup for list pager</returns>
        public static IHtmlContent ListPager(this IHtmlHelper html, string clientReload, string viewName)
        {
            if (html.ViewContext.RouteData.DataTokens.ContainsKey("@Paging"))
            {
                var paging = (PagingSpec)html.ViewContext.RouteData.DataTokens["@Paging"];
                paging.ClientReload = clientReload;
                return html.Partial(viewName, paging);
            }

            return new HtmlString("");
        }

    }
}
