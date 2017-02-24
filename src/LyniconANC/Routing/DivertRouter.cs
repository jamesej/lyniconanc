using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Routing
{
    public class DivertRouter : IRouter
    {
        string controller;
        string action;
        string area = null;
        IRouter innerRouter;
        public DivertRouter(IRouter innerRouter, string controller, string action)
        {
            this.controller = controller;
            this.action = action;
            this.innerRouter = innerRouter;
        }
        public DivertRouter(IRouter innerRouter, string area, string controller, string action) : this(innerRouter, controller, action)
        {
            this.area = area;
        }

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            throw new NotImplementedException();
        }

        public async Task RouteAsync(RouteContext context)
        {
            context.RouteData.RedirectAction(area, controller, action);
            await innerRouter.RouteAsync(context);
        }
    }
}
