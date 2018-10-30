using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Lynicon.Models;
using Lynicon.Utility;
using Lynicon.Collation;
using Microsoft.AspNetCore.Routing;
using Lynicon.Extensibility;
using System.Resources;
using Lynicon.Repositories;
using Microsoft.AspNetCore.Routing.Template;
using Lynicon.Map;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using Lynicon.Editors;

namespace Lynicon.Routing
{
    /// <summary>
    /// Extension methods to do routing
    /// </summary>
    public static class RouteX
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(RouteX));

        public const string CurrentRouteDataKey = "LynCurrRD";

        /// <summary>
        /// Get the current route data from the request cache for places where the route data is not otherwise
        /// available
        /// </summary>
        /// <returns></returns>
        static public RouteData CurrentRouteData()
        {
            if (RequestContextManager.Instance.CurrentContext == null
                || RequestContextManager.Instance.CurrentContext.Items[RouteX.CurrentRouteDataKey] == null)
                return null;
            var rd = (RouteData)RequestContextManager.Instance.CurrentContext.Items[RouteX.CurrentRouteDataKey];
            return rd;
        }

        /// <summary>
        /// Redirect the indicated area, controller and action in a RouteData elsewhere
        /// </summary>
        /// <param name="rd">The RouteData to redirect</param>
        /// <param name="area">The area to redirect to</param>
        /// <param name="controller">The controller to redirect to</param>
        /// <param name="action">The action to redirect to</param>
        static public void RedirectAction(this RouteData rd, string area, string controller, string action)
        {
            if (area != null)
            {
                rd.Values["originalArea"] = rd.DataTokens["Area"] ?? "";
                rd.Values["Area"] = area;
            }
            rd.RedirectAction(controller, action);
        }
        /// <summary>
        /// Redirect the indicated controller and action in a RouteData elsewhere
        /// </summary>
        /// <param name="rd">The RouteData to redirect</param>
        /// <param name="controller">The controller to redirect to</param>
        /// <param name="action">The action to redirect to</param>
        static public void RedirectAction(this RouteData rd, string controller, string action)
        {
            rd.Values["originalController"] = rd.Values["controller"];
            rd.Values["controller"] = controller;
            rd.RedirectAction(action);
        }
        /// <summary>
        /// Redirect the indicated action in a RouteData elsewhere
        /// </summary>
        /// <param name="rd">The RouteData to redirect</param>
        /// <param name="action">The action to redirect to</param>
        static public void RedirectAction(this RouteData rd, string action)
        {
            rd.Values["originalAction"] = rd.Values["action"];
            rd.Values["action"] = action;
        }

        /// <summary>
        /// Create a copy of RouteData
        /// </summary>
        /// <param name="rd">The RouteData to copy</param>
        /// <returns>Copy of the RouteData</returns>
        static public RouteData Copy(this RouteData rd)
        {
            var newRd = new RouteData();
            rd.Routers.Do(r => newRd.Routers.Add(r));
            rd.Values.Do(kvp => newRd.Values.Add(kvp.Key, kvp.Value));
            rd.DataTokens.Do(kvp => newRd.DataTokens.Add(kvp.Key, kvp.Value));

            return newRd;
        }

        /// <summary>
        /// Test whether two RouteDatas have the same set of keys and values in their Values property
        /// </summary>
        /// <param name="rd0">The first RouteData</param>
        /// <param name="rd1">The second RouteData</param>
        /// <returns>True if match</returns>
        static public bool Match(this RouteData rd0, RouteData rd1)
        {
            foreach (var key in rd1.Values.Keys)
                if (!rd0.Values.ContainsKey(key) || !rd0.Values[key].Equals(rd1.Values[key]))
                    return false;

            foreach (var key in rd0.Values.Keys)
                if (!rd1.Values.ContainsKey(key))
                    return false;

            return true;
        }

        /// <summary>
        /// Get the undiverted version from a RouteData that was redirected using RedirectAction
        /// </summary>
        /// <param name="rd">A redirected RouteData</param>
        /// <returns>The original unredirected RouteData (a new object)</returns>
        static public RouteData GetOriginal(this RouteData rd)
        {
            var origRd = rd.Copy();
            origRd.Values["controller"] = rd.Values["originalController"];
            origRd.Values["action"] = rd.Values["originalAction"];
            origRd.DataTokens["area"] = rd.DataTokens["originalArea"];

            return origRd;
        }

        static public IRouteBuilder MapLyniconRoutes(this IRouteBuilder builder)
        {
            LyniconModuleManager.Instance.MapRoutes(builder);
            return builder;
        }

        /// <summary>
        /// Adds a route to the <see cref="IRouteBuilder"/> configured for data fetching, with the specified name, template, default values, and
        /// data tokens.
        /// </summary>
        /// <param name="routeBuilder">The <see cref="IRouteBuilder"/> to add the route to.</param>
        /// <param name="name">The name of the route.</param>
        /// <param name="template">The URL pattern of the route.</param>
        /// <param name="defaults">
        /// An object that contains default values for route parameters. The object's properties represent the names
        /// and values of the default values.
        /// </param>
        /// <param name="constraints">
        /// An object that contains constraints for the route. The object's properties represent the names and values
        /// of the constraints.
        /// </param>
        /// <param name="dataTokens">
        /// An object that contains data tokens for the route. The object's properties represent the names and values
        /// of the data tokens.
        /// </param>
        /// <param name="writePermission">
        /// A permission object which specifies who can create or edit the content item via this route
        /// </param>
        /// <param name="divertOverride">
        /// A function which checks whether to switch out the default inner router with one which diverts the user
        /// to an editor controller
        /// </param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IRouteBuilder MapDataRoute<T>(
            this IRouteBuilder routeBuilder,
            string name,
            string template,
            object defaults = null,
            object constraints = null,
            object dataTokens = null,
            ContentPermission writePermission = null,
            DiversionStrategy divertOverride = null)
            where T : class, new()
        {
            if (routeBuilder.DefaultHandler == null)
            {
                throw new InvalidOperationException("Default handler must be set");
            }

            var inlineConstraintResolver = (IInlineConstraintResolver)routeBuilder
                .ServiceProvider
                .GetService(typeof(IInlineConstraintResolver));

            // Interpose a DataFetchingRouter between the classic Route and the DefaultHandler, which
            // tries to fetch the data for the route
            var dataFetchingRouter = new DataFetchingRouter<T>(routeBuilder.DefaultHandler, false, writePermission, divertOverride);

            var dataRoute = new DataRoute(
                dataFetchingRouter,
                name,
                template,
                new RouteValueDictionary(defaults),
                new RouteValueDictionary(constraints),
                new RouteValueDictionary(dataTokens),
                inlineConstraintResolver);
            routeBuilder.Routes.Add(dataRoute);

            // Record the data route on Lynicon's internal RouteCollection used for reverse url generation
            if (ContentMap.Instance.RouteCollection == null)
                ContentMap.Instance.RouteCollection = new RouteCollection();
            ContentMap.Instance.RouteCollection.Add(dataRoute);

            ContentTypeHierarchy.RegisterType(typeof(T));

            return routeBuilder;
        }

            /* TMP
            /// <summary>
            /// Add a data route which instead of passing content data to the controller, passes a lazy evaluator which when executed fetches the content
            /// data so that the controller can decide whether or not to this fetch is done.
            /// </summary>
            /// <typeparam name="TData">The type of the content data attached by the DataRoute</typeparam>
            /// <param name="routes">A RouteCollection to add the route to</param>
            /// <param name="name">Name of the route table entry</param>
            /// <param name="url">The url matching pattern</param>
            /// <param name="defaults">Default values for unmatched pattern elements</param>
            /// <returns>The DataRoute that was created and registered</returns>
            static public DataRoute<TData> AddLazyDataRoute<TData>(this RouteCollection routes, string name, string url, object defaults)
                where TData : class, new()
            {
                ValidateRouteSpec(name, typeof(TData), url, defaults);
                var route = new DataRoute<TData>(url, new RouteValueDictionary(defaults), new MvcRouteHandler());
                route.LazyData = true;
                routes.Add(name, route);
                return route;
            }


            static public RequestMatchRoute AddRequestMatchRoute(this RouteCollection routes, string name, string url, Func<HttpContextBase, bool> check, Func<string, string> conformUrl)
            {
                var route = new RequestMatchRoute(url, check, conformUrl, new MvcRouteHandler());
                routes.Add(name, route);
                return route;
            }
            static public RequestMatchRoute AddRequestMatchRoute(this RouteCollection routes, string name, string url, Func<HttpContextBase, bool> check, Func<string, string> conformUrl, object defaults)
            {
                var route = new RequestMatchRoute(url, check, conformUrl, new RouteValueDictionary(defaults), new MvcRouteHandler());
                routes.Add(name, route);
                return route;
            }

            static private void ValidateRouteSpec(string name, Type contentType, string url, object defaults)
            {
                bool hasAction = false;
                if (url.Contains("{action}"))
                    hasAction = true;
                if (!hasAction)
                {
                    var actPi = defaults.GetType().GetProperty("action");
                    hasAction = (actPi != null);
                }
                bool hasController = false;
                if (url.Contains("{controller}"))
                    hasController = true;
                if (!hasController)
                {
                    var contrPi = defaults.GetType().GetProperty("controller");
                    hasController = (contrPi != null);
                }
                string error = null;

                if (!(hasAction && hasController))
                    error = "Route " + name + " cannot define both action and controller";


                if (typeof(Summary).IsAssignableFrom(contentType))
                    error = "Data route " + name + " cannot be declared in terms of a summary type";

                if (error != null)
                {
                    log.Fatal(error);
                    throw new ArgumentException(error);
                }
            }
            */

        /// <summary>
        /// Analyzes a route to find the url variables in the url pattern and stores them in a dictionary, with the variable name as the
        /// key and the value as ? for a normal variable and ?? for a catchall
        /// </summary>
        /// <param name="route">the route to analyze</param>
        /// <returns>dictionary of variables</returns>
        static public Dictionary<string,string> KeyOutputs(this Route route)
        {
            var rt = TemplateParser.Parse(route.RouteTemplate);

            //var varsx = route.RouteTemplate.Split('{')
            //    .Where(s => s.Contains("}"))
            //    .Select(s => s.UpTo("}"))
            //    .ToDictionary(s => s.StartsWith("*") ? s.Substring(1).ToLower() : s.ToLower(), s => s.StartsWith("*") ? "??" : "?");
            var vars = rt.Parameters.ToDictionary(p => p.Name, p => p.IsCatchAll ? "??" : "?");
            foreach (var d in route.Defaults)
            {
                if (vars.ContainsKey(d.Key)) // variable with default
                {
                    if (rt.GetParameter(d.Key).IsOptional)
                        vars[d.Key.ToLower()] = "??";
                    else
                        vars[d.Key.ToLower()] = "?" + d.Value.ToString();
                }
                else
                    vars[d.Key.ToLower()] = d.Value.ToString().ToLower();
            }
            return vars;
        }

        public static IEnumerable<string> GetTemplatePatterns(this RouteData rd, Type contentType)
        {
            return rd.Routers[0].GetTemplatePatterns(contentType, null).Distinct();
        }

        public static IEnumerable<string> GetTemplatePatterns(this IRouter router, Type contentType, RouteData rd)
        {
            if (router is DataRoute && ((DataRoute)router).DataType == contentType)
                foreach (string tp in ((DataRoute)router).GetTemplatePatterns(rd))
                    yield return tp;
            else if (router is RouteCollection)
            {
                var patterns = new List<string>();
                for (int i = 0; i < ((RouteCollection)router).Count; i++)
                {
                    foreach (string tp in ((RouteCollection)router)[i].GetTemplatePatterns(contentType, rd))
                        yield return tp;
                }
            }
        }
        public static IEnumerable<string> GetTemplatePatterns(this DataRoute route, RouteData rd)
        {
            string sTemplate = route.RouteTemplate;
            var template = TemplateParser.Parse(sTemplate);

            var sbPatt = new StringBuilder();
            foreach (string pattEl in sTemplate.Split('{'))
            {
                if (!pattEl.Contains("}"))
                {
                    sbPatt.Append(pattEl);
                    continue;
                }

                string key = pattEl.UpTo("}");
                string remaining = pattEl.After("}");
                bool isCatchAll = key.StartsWith("*");
                if (isCatchAll)
                    key = key.Substring(1);
                key = key.UpTo(":").UpTo("=").UpTo("?"); // trim off optional marker,constraints or defaults
                if (key == "action")
                {
                    var actions = ContentTypeHierarchy.ControllerActions[rd.Values["controller"].ToString()];
                    if (!actions.Contains(rd.Values["action"].ToString().ToLower())) // if keyOutputs["action"] starts with "?" it has a default value and can be omitted, so the default action is assumed
                        continue;
                    sbPatt.Append("{");
                    sbPatt.Append(actions.Join("|"));
                    sbPatt.Append("}");
                }
                else
                {
                    string keyVal = "";
                    if (rd != null && rd.Values.ContainsKey(keyVal))
                        keyVal = rd.Values[key].ToString();
                    if (key.StartsWith("_"))
                    {
                        if (isCatchAll)
                            sbPatt.Append("{/" + keyVal + "}"); // catchall, can be empty and contain /
                        else if (template.GetParameter(key).IsOptional)
                            sbPatt.Append("{?" + keyVal + "}"); // mandatory, can't be empty, no /s
                        else
                            sbPatt.Append("{*" + keyVal + "}"); // optional, can be empty at RH end of url, no /s

                    }
                    else
                        sbPatt.Append(keyVal == "" ? "_" + key + "_" : keyVal); // if no value supplied, use key as a dummy url element
                }
                sbPatt.Append(remaining);
            }

            string ps = Regex.Replace(sbPatt.ToString(), "/+", "/");
            ps = Regex.Replace(ps, "/$", "");
            yield return ps;
        }

        /// <summary>
        /// Generates a string defining how to generate a UI for filling in the variables in a url pattern
        /// </summary>
        /// <param name="route">The route whose url pattern is used</param>
        /// <param name="rd">The route data providing the initial values for the url variables</param>
        /// <returns>a string defining how to generate a UI for filling in the variables in a url pattern</returns>
        public static string GetUrlPattern(this Route route, RouteData rd)
        {
            var patt = route.RouteTemplate;
            var keyOutputs = route.KeyOutputs();

            if (true) //TODO revise logic route.RouteHandler is MvcRouteHandler)
            {
                if (!(keyOutputs.ContainsKey("controller") && keyOutputs.ContainsKey("action")))
                    throw new Exception("Route " + route.RouteTemplate + " fails to define controller and action");

                if (keyOutputs["controller"].StartsWith("?"))
                    throw new Exception("Route " + route.RouteTemplate + " is a data route which lacks but must have a specified controller");

                if (!ContentTypeHierarchy.ControllerActions.ContainsKey(keyOutputs["controller"]))
                    return null;
            }

            var sbPatt = new StringBuilder();
            foreach (string pattEl in patt.Split('{'))
            {
                if (!pattEl.Contains("}"))
                {
                    sbPatt.Append(pattEl);
                    continue;
                }

                string key = pattEl.UpTo("}");
                string remaining = pattEl.After("}");
                bool isCatchAll = key.StartsWith("*");
                if (isCatchAll)
                    key = key.Substring(1);
                if (key == "action")
                {

                    var actions = ContentTypeHierarchy.ControllerActions[keyOutputs["controller"]];
                    if (!actions.Contains(keyOutputs["action"].ToLower())) // if keyOutputs["action"] starts with "?" it has a default value and can be omitted, so the default action is assumed
                        continue;
                    sbPatt.Append("{");
                    sbPatt.Append(actions.Join("|"));
                    sbPatt.Append("}");
                }
                else
                {
                    string keyVal = "";
                    if (rd != null)
                        keyVal = rd.Values[key].ToString();
                    if (key.StartsWith("_"))
                    {
                        if (isCatchAll)
                            sbPatt.Append("{/" + keyVal + "}"); // catchall, can be empty and contain /
                        else if (keyOutputs[key] == "?")
                            sbPatt.Append("{?" + keyVal + "}"); // mandatory, can't be empty, no /s
                        else
                            sbPatt.Append("{*" + keyVal + "}"); // optional, can be empty at RH end of url, no /s
                            
                    }
                    else
                        sbPatt.Append(keyVal);
                }
                sbPatt.Append(remaining);
            }

            string ps = Regex.Replace(sbPatt.ToString(), "/+", "/");
            ps = Regex.Replace(ps, "/$", "");
            return ps;
        }

        /// <summary>
        /// Process a url through the route table to turn it into a RouteData
        /// </summary>
        /// <param name="url">The url</param>
        /// <returns>The route data generated if the url was in a request to the site</returns>
        public static RouteData GetRouteDataByUrl(string url)
        {
            url = MakeUrlAbsolute(url);   
            var routeContext = new RouteContext(new MockHttpContext(url));
            ContentMap.Instance.RouteCollection.RouteAsync(routeContext);
            return routeContext.RouteData;
        }

        public static string MakeUrlAbsolute(string url)
        {
            Uri result;
            bool isAbsolute = Uri.TryCreate(url, UriKind.Absolute, out result);

            if (!(url.StartsWith("/") || url.StartsWith("~/") || isAbsolute))
                url = "/" + url;
            if (RequestContextManager.Instance.CurrentContext != null && !url.StartsWith("http"))
            {
                var req = RequestContextManager.Instance.CurrentContext.Request;
                url = req.Scheme + "://" + req.Host + url;
            }
            else if (!isAbsolute)
                url = "http://www.test.com" + url;

            return url;
        }

        /// <summary>
        /// Process a url through the route table to find what content address it would request content from
        /// </summary>
        /// <param name="url">The url</param>
        /// <returns>The address from which content would be requested if that url was in a request to the site</returns>
        public static Address GetAddressByUrl(string url)
        {
            RouteData urlRd = RouteX.GetRouteDataByUrl(url);
            if (urlRd.Routers[0] is DataRoute && urlRd.Routers[1] is DataFetchingRouter)
            {
                Type routeType = urlRd.Routers[1].GetType().GetGenericArguments()[0];
                return new Address(routeType, urlRd);
            }

            return null;
        }


        public static string CreateUrlFromRouteValues(RouteValueDictionary routeValues)
        {
            var vpc = new VirtualPathContext(new MockHttpContext(""), new RouteValueDictionary(), routeValues);
            var virtualPathData = ContentMap.Instance.RouteCollection.GetVirtualPath(vpc);
            if (virtualPathData == null)
            {
                return null;
            }
            return virtualPathData.VirtualPath;
        }
    }
}
