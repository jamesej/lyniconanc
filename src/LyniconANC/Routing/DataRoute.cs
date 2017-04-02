using Lynicon.Collation;
using Lynicon.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Routing
{
    public class DataRoute : Route
    {
        private readonly DataFetchingRouter target;

        public DataRoute(
            DataFetchingRouter target,
            string routeName,
            string routeTemplate,
            RouteValueDictionary defaults,
            IDictionary<string, object> constraints,
            RouteValueDictionary dataTokens,
            IInlineConstraintResolver inlineConstraintResolver)
            : base(
                  (IRouter)target,
                  routeName,
                  routeTemplate,
                  defaults,
                  constraints,
                  dataTokens,
                  inlineConstraintResolver)
        {
            this.target = target;
        }

        public override Task RouteAsync(RouteContext context)
        {
            return base.RouteAsync(context);
        }

        public virtual Task RouteBypassingCriteriaAsync(RouteContext context)
        {
            return target.RouteAsync(context);
        }

        public override VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            if (context.AmbientValues.ContainsKey("data"))
                context.AmbientValues.Remove("data");

            var vpd = base.GetVirtualPath(context);
            return vpd;
        }

        public Type DataType
        {
            get
            {
                if (!target.GetType().IsGenericType())
                    return null;
                return target.GetType().GenericTypeArguments[0];
            }
        }
    }
}
