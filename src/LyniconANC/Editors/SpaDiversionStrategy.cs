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

        /// <summary>
        /// Routes as follows:
        /// POST + can't write => Unauthorized
        /// API req + no data => Not Found
        /// API req + data => Return content data (JSON)
        /// HTML req + can't write => Drop through (to SPA route to show page)
        /// Non editing => Drop through (to SPA route to show page)
        /// => Editor, but drop through if no data and can't create new
        /// </summary>
        /// <param name="innerRouter"></param>
        /// <param name="context"></param>
        /// <param name="data"></param>
        /// <param name="writePermission"></param>
        /// <returns>(router to use, drop through if can't use router)</returns>
        public override (IRouter, bool) GetDiversion(IRouter innerRouter, RouteContext context, object data, ContentPermission writePermission)
        {
            bool acceptsHtml = AcceptsHtml(context.HttpContext.Request);
            bool isPost = (context.HttpContext.Request.Method == "POST");

            var actualWritePermission = writePermission ?? this.writePermission;
            bool canWrite = actualWritePermission.Permitted(data);

            if (isPost && !canWrite)
                return (new DivertRouter(innerRouter, "Lynicon", "Api", "ErrorUnauthorized"), false);

            if (acceptsHtml)
                return GetDiversionHtml(innerRouter, context, data, canWrite);
            else
                return GetDiversionApi(innerRouter, context, data, canWrite);
        }

        private (IRouter, bool) GetDiversionHtml(IRouter innerRouter, RouteContext context, object data, bool canWrite)
        {
            if (!canWrite)
                return (null, true); // drop through as SPA catch all should handle this request: a non-editor wants to read html

            string action = ActionByModeFlag(context, data, canWrite) ?? "Index";
            if (action == "")
                return (null, true);

            // means drop through if no data and not creating new data
            return (new DivertRouter(innerRouter, editorArea, editorController, action), true);
        }

        private (IRouter, bool) GetDiversionApi(IRouter innerRouter, RouteContext context, object data, bool canWrite)
        {
            if (data == null)
                return (new DivertRouter(innerRouter, "Lynicon", "Api", "ErrorNotFound"), false);
            else
                return (new DivertRouter(innerRouter, ApiControllerArea, ApiController, "Index"), false);
        }
    }
}
