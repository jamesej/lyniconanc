using Lynicon.Editors;
using Lynicon.Extensibility;
using Lynicon.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyniconANC.Test
{
    public static class RouteCollectionX
    {
        public static void AddTestDataRoute<T>(this RouteCollection routes, string name, string template, object defaults,
            ContentPermission writePermission = null, DiversionStrategy divertOverride = null) where T : class, new()
        {
            IOptions<RouteOptions> routeOpts = Options.Create<RouteOptions>(new RouteOptions());
            var constraintResolver = new DefaultInlineConstraintResolver(routeOpts);
            var dataFetchingRouter = new DataFetchingRouter<T>(new MockRouter(), false, writePermission, divertOverride);
            var dataRoute = new DataRoute(
                dataFetchingRouter,
                name,
                template,
                new RouteValueDictionary(defaults),
                new RouteValueDictionary(null),
                new RouteValueDictionary(null),
                constraintResolver);

            routes.Add(dataRoute);
        }
    }
}
