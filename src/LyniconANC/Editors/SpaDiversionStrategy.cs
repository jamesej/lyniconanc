using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lynicon.Extensibility;
using Lynicon.Membership;
using Lynicon.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lynicon.Editors
{
    public class SpaDiversionStrategy : DiversionStrategy
    {
        const string ApiControllerArea = "Lynicon";
        const string ApiController = "Api";

        readonly string editorController;
        readonly string editorArea;
        readonly ContentPermission writePermission;

        public SpaDiversionStrategy(string editorArea, string editorController, ContentPermission writePermission = null)
        {
            this.editorController = editorController;
            this.editorArea = editorArea;
            this.writePermission = writePermission ?? new ContentPermission(User.EditorRole);
        }

        public override (IRouter, bool) GetDiversion(IRouter innerRouter, RouteContext context, object data, ContentPermission writePermission)
        {
            bool acceptsHtml = AcceptsHtml(context.HttpContext.Request);
            bool isPost = (context.HttpContext.Request.Method == "POST");

            var actualWritePermission = writePermission ?? this.writePermission;
            bool canWrite = actualWritePermission.Permitted(data);

            if (isPost && !canWrite)
                return (new DivertRouter(innerRouter, "Api", "Unauthorized"), false);

            if (!acceptsHtml && data == null)
                return (new DivertRouter(innerRouter, "Api", "NotFound"), false);

            if (!acceptsHtml)
                return (new DivertRouter(innerRouter, ApiControllerArea, ApiController, "Index"), false);
            if (acceptsHtml && !canWrite)
                return (null, true); // drop through as SPA catch all should handle this request: a non-editor wants to read html

            string action = ActionByModeFlag(context, data, canWrite) ?? "Index";
            if (action == "")
                return (null, true);
            return (new DivertRouter(innerRouter, editorArea, editorController, action), false);
        }
    }
}
