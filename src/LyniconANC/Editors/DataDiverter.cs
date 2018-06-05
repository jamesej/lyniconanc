
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
using Microsoft.Extensions.Primitives;

namespace Lynicon.Editors
{
    /// <summary>
    /// Common functionality for deciding on editor redirection
    /// </summary>
    public class DataDiverter : TypeRegistry<DiversionStrategy>
    {
        static DataDiverter instance = new DataDiverter();
        public static DataDiverter Instance { get { return instance; } internal set { instance = value; } }

        public DataDiverter()
        {
            this.DefaultHandler = new EditorDiversionStrategy("Lynicon", "DualFrameEditor");
            Register(typeof(List<>), new EditorDiversionStrategy("Lynicon", "ListEditor"));
        }

        public IRouter NullDivert(IRouter router, RouteContext context, object data) => null;

        public override DiversionStrategy Registered(Type type)
        {
            if (base.Registered(type) == this.DefaultHandler && type.IsGenericType() && type.GetGenericTypeDefinition() == typeof(List<>))
                return Registered(typeof(List<>));

            return base.Registered(type);
        }

    }
}
