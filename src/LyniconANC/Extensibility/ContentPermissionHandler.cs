using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Extensibility
{
    public class ContentPermissionHandler : AuthorizationHandler<ContentPermission>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ContentPermission requirement)
        {
            var filtContext = context.Resource as AuthorizationFilterContext;
            object data;
            if (filtContext != null)
                data = filtContext.RouteData.Values["data"];
            else
                data = context.Resource;
            var user = context.User;
            if (requirement.Permitted(data, user))
                context.Succeed(requirement);
            return Task.FromResult(0);
        }
    }

}
