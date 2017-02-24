using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyniconANC.Test
{
    public class MockRouter : IRouter
    {
        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            return null;
        }

        public Task RouteAsync(RouteContext context)
        {
            context.Handler = new RequestDelegate(ctx => Task.FromResult<int>(0));
            return Task.FromResult<int>(0);
        }
    }
}
