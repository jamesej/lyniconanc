using Lynicon.Models;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Utility;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Editors;
using System.Collections;

namespace Lynicon.Routing
{
    public abstract class DataFetchingRouter
    {
        public static Func<Type, bool> TypeCheckExistenceBySummary = (t => false);

        public abstract Task RouteAsync(RouteContext context);

        public abstract VirtualPathData GetVirtualPath(VirtualPathContext context);
    }

    public class DataFetchingRouter<T> : DataFetchingRouter, IRouter where T : class, new()
    {
        IRouter target;
        public bool LazyData { get; set; }
        ContentPermission writePermission;
        public DiversionStrategy DivertOverride { get; set; }

        public DataFetchingRouter(IRouter target, bool lazyData = false, ContentPermission writePermission = null, DiversionStrategy divertOverride = null)
        {
            this.target = target;
            this.LazyData = lazyData;
            this.DivertOverride = divertOverride;
            this.writePermission = writePermission;
        }

        public override VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            return null;
        }

        public override async Task RouteAsync(RouteContext context)
        {
            Dictionary<string, StringValues> qsParams = context.HttpContext.Request.Query.Keys
                .Where(key => key != null)
                .ToDictionary(key => key, key => context.HttpContext.Request.Query[key]);

            bool typeSpecified = false;

            // Deal with type restrictor query parameter
            if (qsParams.ContainsKey("$type") || qsParams.ContainsKey("$create"))
            {
                string typeSpec = qsParams.ContainsKey("$type") ? qsParams["$type"][0] : qsParams["$create"][0];
                typeSpec = typeSpec.ToLower();
                Type type = typeof(T);
                if (typeof(IList).IsAssignableFrom(type) && type.IsGenericType)
                {
                    type = type.GetGenericArguments()[0];
                }
                if (typeSpec.Contains("."))
                {
                    if (type.FullName.ToLower() != typeSpec)
                        return;
                }
                else
                {
                    string typeName = type.Name.ToLower();
                    if (typeName.EndsWith("content"))
                        typeName = typeName.UpToLast("content");
                    if (typeName != typeSpec && typeName + "content" != typeSpec)
                        return;
                }

                typeSpecified = true;
            }

            var specialQueryParams = new List<string> { "$filter" };
            specialQueryParams.AddRange(PagingSpec.PagingKeys);
            qsParams
                .Where(kvp => specialQueryParams.Contains(kvp.Key))
                .Do(kvp => context.RouteData.DataTokens.Add(kvp.Key, kvp.Value[0]));

            // current version may be based on route data if it has addressable elements
            VersionManager.Instance.InvalidateCurrentVersion();

            var data = GetData(context.RouteData);

            var ied = (DataRouteInterceptEventData)EventHub.Instance.ProcessEvent("DataRoute.Intercept", this, new DataRouteInterceptEventData
            {
                QueryStringParams = qsParams,
                Data = data,
                ContentType = typeof(T),
                RouteData = context.RouteData,
                WasHandled = false
            }).Data;

            // stop here if the request was intercepted
            if (ied.WasHandled)
            {
                if (ied.RouteData != null)
                    await target.RouteAsync(context);
                return;
            }

            var divertStrategy = this.DivertOverride ?? DataDiverter.Instance.Registered<T>();
            (var divertRouter, bool dropThrough) = divertStrategy.GetDiversion(target, context, data, writePermission);

            if (data == null)
            {
                // Request has specified a type so we can create knowing it won't cause a problem with
                // the case where multiple types exist on the same (or overlapping) template(s)
                if (typeSpecified && divertRouter != null)
                {
                    data = Collator.Instance.GetNew<T>(context.RouteData);
                    context.RouteData.DataTokens.Add("LynNewItem", true);
                }
                else if (divertRouter == null || dropThrough) // no data and no diversion: drop through
                    return;
            }

            context.RouteData.Values.Add("data", data);

            if (divertRouter != null)
                await divertRouter.RouteAsync(context);
            else if (dropThrough)
                return;
            else
                await target.RouteAsync(context);
        }

        protected virtual object GetData(RouteData rd)
        {
            // versioning may depend on route (for addressable versions) so we need to ensure the current
            // version is the one for the route we are currently on
            var currentForRoute = VersionManager.Instance.GetCurrentVersionForRoute(rd);
            using (var ctx = VersionManager.Instance.PushState(currentForRoute))
            {
                if (DataFetchingRouter.TypeCheckExistenceBySummary(typeof(T)))
                {
                    bool isList = typeof(T).IsGenericType() && typeof(T).GetGenericTypeDefinition() == typeof(List<>);
                    if (!isList)
                    {
                        Summary summ = Collator.Instance.Get<Summary>(typeof(T), rd);
                        if (summ == null)
                            return null;
                    }
                }
                if (LazyData)
                    return new Lazy<T>(() => Collator.Instance.Get<T>(rd));
                else
                    return Collator.Instance.Get<T>(rd);
            }
        }
    }
}
