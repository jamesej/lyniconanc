
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Utility;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Lynicon.Routing;
using Lynicon.Membership;

namespace Lynicon.Editors
{
    /// <summary>
    /// Common functionality for deciding on editor redirection
    /// </summary>
    public class DataDiverter : TypeRegistry<Func<IRouter, RouteContext, object, IRouter>>
    {
        static readonly DataDiverter instance = new DataDiverter();
        public static DataDiverter Instance { get { return instance; } }

        public DataDiverter()
        {
            this.DefaultHandler = EditorDivert("Lynicon", "DualFrameEditor");
            Register(typeof(List<>), EditorDivert("Lynicon", "ListEditor"));
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

        protected virtual IRouter EditorDivertOuter(string area, string controller, IRouter innerRouter, RouteContext context, object data)
        {
            bool isEditor = SecurityManager.Current == null ? false : SecurityManager.Current.CurrentUserInRole("E");
            string action = ActionByModeFlag(context, data, isEditor) ?? "Index";
            if (action == "")
                return null;
            return new DivertRouter(innerRouter, area, controller, action);
        }

        public Func<IRouter, RouteContext, object, IRouter> EditorDivert(string area, string controller)
        {
            return (innerRouter, context, data) => EditorDivertOuter(area, controller, innerRouter, context, data);
        }

        public IRouter NullDivert(IRouter router, RouteContext context, object data) => null;

        public override Func<IRouter, RouteContext, object, IRouter> Registered(Type type)
        {
            if (base.Registered(type) == this.DefaultHandler && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return Registered(typeof(List<>));

            return base.Registered(type);
        }

    }
}
