using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Routing
{
    /// <summary>
    /// Data sent to DataRoute.Intercept event raised when DataRoute is processing a route
    /// </summary>
    public class DataRouteInterceptEventData
    {
        /// <summary>
        /// Parameters from the query string
        /// </summary>
        public Dictionary<string, StringValues> QueryStringParams { get; set; }
        /// <summary>
        /// The content item found (or null if not)
        /// </summary>
        public object Data { get; set; }
        /// <summary>
        /// The content type of the DataRoute
        /// </summary>
        public Type ContentType { get; set; }
        /// <summary>
        /// The underlying RouteData match has resulted in this RouteData
        /// </summary>
        public RouteData RouteData { get; set; }
        /// <summary>
        /// Whether an event processor has completed processing for this DataRoute and it can
        /// stop here, returning the RouteData property
        /// </summary>
        public bool WasHandled { get; set; }
    }
}
