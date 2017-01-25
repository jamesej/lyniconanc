using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Microsoft.AspNetCore.Routing;

namespace Lynicon.Routing
{
    /// <summary>
    /// UrlRequestInterceptor provides a processor for DataRoute.Intercept when Register is run,
    /// in which certain special operations are performed on urls handled by data routes when they
    /// have special query string arguments (by convention these arguments begin with a $).
    /// </summary>
    public class UrlRequestInterceptor
    {
        /// <summary>
        /// Register event processing for url interception
        /// </summary>
        public static void Register()
        {
            EventHub.Instance.RegisterEventProcessor("DataRoute.Intercept",
                ehd => ProcessIntercept(ehd), "UrlRequestInterceptor");
        }

        private static object ProcessIntercept(EventHubData ehd)
        {
            var ed = (DataRouteInterceptEventData)ehd.Data;
            if (ed.WasHandled) return ed;
            var route = ed.RouteData.Routers.OfType<Route>().First();

            if (ed.QueryStringParams.ContainsKey("$urlget"))
            {
                if (ed.Data == null)
                    return ed;
                else
                {
                    ed.RouteData.RedirectAction("Lynicon", "UrlManager", "Index");
                    ed.RouteData.DataTokens.Add("$urlget", route.GetUrlPattern(ed.RouteData));
                    ed.WasHandled = true;
                }
            }
            else if (ed.QueryStringParams.ContainsKey("$urlset"))
            {
                if (ed.Data != null)
                {
                    ed.RouteData.RedirectAction("Lynicon", "UrlManager", "AlreadyExists");
                }
                else
                {
                    ed.RouteData.RedirectAction("Lynicon", "UrlManager", "MoveUrl");
                    ed.RouteData.DataTokens.Add("$urlset", ed.QueryStringParams["$urlset"][0]);
                    ed.RouteData.DataTokens.Add("Type", ed.ContentType);
                }
                ed.WasHandled = true;
            }
            else if (ed.QueryStringParams.ContainsKey("$urldelete"))
            {
                if (ed.Data == null)
                    return ed;
                else
                {
                    ed.RouteData.RedirectAction("Lynicon", "UrlManager", "Delete");
                    ed.RouteData.Values.Add("data", ed.Data);
                    ed.WasHandled = true;
                }
            }
            else if (ed.QueryStringParams.ContainsKey("$urlverify"))
            {
                if (ed.Data == null)
                    return ed;
                else
                {
                    ed.RouteData.RedirectAction("Lynicon", "UrlManager", "VerifyExists");
                    ed.WasHandled = true;
                }
            }

            return ed;
        }
    }
}
