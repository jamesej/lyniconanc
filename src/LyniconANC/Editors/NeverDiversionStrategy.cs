using System;
using System.Collections.Generic;
using System.Text;
using Lynicon.Extensibility;
using Microsoft.AspNetCore.Routing;

namespace Lynicon.Editors
{
    public class NeverDiversionStrategy : DiversionStrategy
    {
        public override (IRouter, bool) GetDiversion(IRouter innerRouter, RouteContext context, object data, ContentPermission writePermission)
        {
            return (null, false);
        }
    }
}
