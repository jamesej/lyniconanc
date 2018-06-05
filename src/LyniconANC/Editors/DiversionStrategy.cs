using Lynicon.Extensibility;
using Lynicon.Membership;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lynicon.Editors
{
    public abstract class DiversionStrategy
    {
        public abstract (IRouter, bool) GetDiversion(IRouter innerRouter, RouteContext context, object data, ContentPermission writePermission);

        public bool AcceptsHtml(HttpRequest request)
        {
            var acceptHeaders = request.GetTypedHeaders().Accept;
            return acceptHeaders == null || acceptHeaders.Any(mt => mt.MediaType == "*/*" || mt.MediaType == "text/html" || mt.MediaType == "application/xhtml+xml");
        }

        public virtual string ActionByModeFlag(RouteContext context, object data, bool permitted)
        {
            HttpContext httpContext = context.HttpContext;
            var query = httpContext?.Request.Query;
            string modeFlag;
            if (query != null && query.ContainsKey("$mode"))
                modeFlag = query["$mode"][0].ToLower();
            else
                modeFlag = "";

            if (modeFlag == "ping")
                return "Ping";
            if (!permitted || modeFlag == "bypass" || (modeFlag == "view" && data != null))
                return "";
            switch (modeFlag)
            {
                case "editor":
                    return "Editor";
                case "view":
                    return "Empty";
                case "property-item-html":
                    return "PropertyItemHtml";
                case "delete":
                    return "Delete";
                case "getvalues":
                    return "GetValues";
            }
            return null;
        }
    }
}
