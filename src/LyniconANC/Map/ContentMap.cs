using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Models;
using Lynicon.Repositories;
using Lynicon.Routing;
using Lynicon.Utility;
using Microsoft.AspNetCore.Routing;
using System.Reflection;
using Lynicon.Services;

namespace Lynicon.Map
{
    /// <summary>
    /// The ContentMap provides methods for determining the url or route data for content items
    /// </summary>
    public class ContentMap
    {
        static readonly ContentMap instance = new ContentMap();
        public static ContentMap Instance { get { return instance; } }

        /// <summary>
        /// Function to test whether a given route data value (which could be null) indicates there is no content at the location
        /// </summary>
        public Func<RouteData, bool> RouteIsNotOccupied { get; set; }

        public RouteCollection RouteCollection { get; set; }

        public ContentMap()
        {
            RouteIsNotOccupied = rd => rd == null;
            RouteCollection = new RouteCollection();
        }
        /// <summary>
        /// Get the url of a container or content item
        /// </summary>
        /// <param name="o">container or content item</param>
        /// <returns>The primary url</returns>
        public string GetUrl(object o)
        {
            return GetUrls(o, true).OrderBy(url => url.Length).FirstOrDefault();
        }
        /// <summary>
        /// Get the url of a container or content item with option to process through
        /// processes registered to GenerateUrl event
        /// </summary>
        /// <param name="o">container or content item</param>
        /// <param name="transform">whether to process through GenerateUrl event</param>
        /// <returns>The primary url</returns>
        public string GetUrl(object o, bool transform)
        {
            return GetUrls(o, transform).OrderBy(url => url.Length).FirstOrDefault();
        }
        /// <summary>
        /// Get the url(s) of a container or content item with option to process through
        /// processes registered to GenerateUrl event
        /// </summary>
        /// <param name="o">container or content item</param>
        /// <param name="transform">whether to process through GenerateUrl event</param>
        /// <returns>All the possible urls</returns>
        public IEnumerable<string> GetUrls(object o, bool transform)
        {
            Type type = Collator.GetContentType(o);

            ICollator collator = Collator.Instance.Registered(type);
            var address = collator.GetAddress(o);

            var container = Collator.Instance.GetContainer(o);
            ItemVersion iv = new ItemVersion(LyniconSystem.Instance, container);

            address = VersionManager.Instance.GetVersionAddress(address, iv);

            var urls = GetUrls(address);

            if (transform)
            {
                urls = urls.Select(url => transform ? UrlTransform(url, iv) : url);
            }

            return urls;
        }
        /// <summary>
        /// Get the url(s) of a content address
        /// </summary>
        /// <param name="address">The address</param>
        /// <returns>List of urls corresponding to the address</returns>
        public IEnumerable<string> GetUrls(Address address)
        {
            List<string> urls = new List<string>();

            for (int i = 0; i < RouteCollection.Count; i++)
            {
                var route = (DataRoute)RouteCollection[i];
                if (!route.DataType.IsAssignableFrom(address.Type)) // possible to have data route with parent class of content class
                    continue;

                urls.AddRange(GetUrls((DataRoute)RouteCollection[i], address));
            }

            return urls.Distinct().OrderBy(url => url.Split('/').Length);
        }

        /// <summary>
        /// Transform a url via processors on GenerateUrl
        /// </summary>
        /// <param name="url">The url to process</param>
        /// <param name="iv">The ItemVersion which is the context in which to process the url</param>
        /// <returns>The processed url</returns>
        public string UrlTransform(string url, ItemVersion iv)
        {
            var tpl = (Tuple<string, ItemVersion>)EventHub.Instance.ProcessEvent("GenerateUrl", this, Tuple.Create(url, iv)).Data;
            return tpl.Item1;
        }

        /// <summary>
        /// Get the possible route datas that could have addressed a given container
        /// </summary>
        /// <param name="container">container</param>
        /// <returns>IEnumerable of the possible route datas</returns>
        public IEnumerable<RouteData> GetRouteDatas(object container)
        {
            return GetRouteDatas(Collator.Instance.GetAddress(container));
        }
        /// <summary>
        /// Get the possible route datas that could have produced a given address
        /// </summary>
        /// <param name="address">The address</param>
        /// <returns>IEnumerable of the possible route datas</returns>
        public IEnumerable<RouteData> GetRouteDatas(Address address)
        {
            for (int i = 0; i < RouteCollection.Count; i++)
            {
                var route = (DataRoute)RouteCollection[i];
                if (!route.DataType.IsAssignableFrom(address.Type)) // possible to have data route with parent class of content class
                    continue;
                var rds = GetRouteDatas(route, address);
                foreach (var rd in rds)
                    yield return rd;
            }
        }

        /// <summary>
        /// Get the possible urls that could have selected a given container or content item via a given route
        /// </summary>
        /// <param name="route">The route</param>
        /// <param name="address">The content address</param>
        /// <returns>The urls that could have accessed the object via the route</returns>
        public List<string> GetUrls(DataRoute route, Address address)
        {
            // Start list of urls which could map to this item.  Note, catchall items (starting *) are treated as normal ones
            List<string> urls = new List<string> { route.RouteTemplate.Replace("{*", "{") };

            var keyOutputs = route.KeyOutputs();

            List<string> replaceWiths = null;

            // Action & controller

            if (keyOutputs.ContainsKey("controller") && keyOutputs.ContainsKey("action"))
            {
                if (keyOutputs["controller"].StartsWith("?"))
                    throw new Exception("Route " + route.RouteTemplate + " is a data route which lacks but must have a specified controller");

                if (!ContentTypeHierarchy.ControllerActions.ContainsKey(keyOutputs["controller"]))
                    return new List<string>();  // Controller doesn't exist therefore no urls

                // copy list so we can change 'actions'
                var actions = ContentTypeHierarchy.ControllerActions[keyOutputs["controller"]].ToList();
                if (keyOutputs["action"].StartsWith("?"))
                {
                    if (keyOutputs["action"].Length > 1 && actions.Contains(keyOutputs["action"].Substring(1).ToLower()))
                        actions.Add("??");
                    UrlX.PermuteReplace(urls, "{action}", actions);
                }
                else if (!actions.Contains(keyOutputs["action"]))
                    return new List<string>(); // Fixed action doesn't exist therefore no urls
            }

            // Route vars addressing the content item

            foreach (var kvp in keyOutputs)
            {
                if (kvp.Key == "controller" || kvp.Key == "action") // already dealt with
                    continue;

                if (kvp.Value.StartsWith("?") && kvp.Value != "??") // variable takes any value
                {
                    if (address.ContainsKey(kvp.Key)) // its part of the content item address
                    {
                        // path element is the default value, so this url element is optional
                        replaceWiths = new List<string> { address.GetAsString(kvp.Key) };
                        if (kvp.Value.Length > 1 && address.GetAsString(kvp.Key) == kvp.Value.Substring(1))
                            replaceWiths.Add("??");
                        // matches a path element
                        address.SetMatched(kvp.Key);   // this address element is matched
                    }
                    else
                    {
                        replaceWiths = new List<string> { "?" };// no match, so many urls mapped to this item
                        if (kvp.Value.Length > 1) // there's a default value so the element is optional
                            replaceWiths.Add("??");
                    }
                    UrlX.PermuteReplace(urls, "{" + kvp.Key + "}", replaceWiths);
                }
                else if (kvp.Value == "??")
                {
                    UrlX.PermuteReplace(urls, "{" + kvp.Key + "}", new List<string> { "??" });   // optional variable
                }
                else // fixed value, has to be matched by item
                {
                    bool matched = false;
                    if (address.ContainsKey(kvp.Key))
                    {
                        if (address.GetAsString(kvp.Key) == kvp.Value)
                        {
                            matched = true;
                            address.SetMatched(kvp.Key);
                        }
                    }
                    if (!matched)
                        return new List<string>();
                }
            }

            if (!address.FullyMatched) // Fails because one or more address elements could not have come from this route
                return new List<string>();

            // Deal with possible url variations with omitted path elements

            for (int i = 0; i < urls.Count; i++)
            {
                while (urls[i].EndsWith("/??"))
                    urls[i] = urls[i].Substring(0, urls[i].Length - 3);
                if (urls[i].Contains("??"))
                    urls[i] = null;
            }

            return urls.Where(u => u != null).OrderBy(u => u.Split('/').Length).Select(u => "/" + u).ToList();
        }

        /// <summary>
        /// Take a set of route value dictionaries, and generate a new set for each value in a list of values
        /// by adding a key with that value to all of the original ones and combining all the resulting sets
        /// </summary>
        /// <param name="rvds">the original set of route value dictionaries</param>
        /// <param name="key">the key to add to all of them</param>
        /// <param name="values">the values to add for this key in each duplicate set</param>
        /// <returns>All the route value dictionaries resulting</returns>
        public List<RouteValueDictionary> PermuteAdd(List<RouteValueDictionary> rvds, string key, List<string> values)
        {
            return rvds
                .SelectMany(rvd => values.Select(v => {
                    var newRvd = new RouteValueDictionary(rvd);
                    newRvd.Add(key, v);
                    return newRvd;
                }))
                .ToList();
        }

        /// <summary>
        /// Get the possible route datas that could have selected a given container or content item via a route
        /// </summary>
        /// <param name="route">The route</param>
        /// <param name="o">The container or content item</param>
        /// <returns>The route datas that could have accessed the object via the route</returns>
        public List<RouteData> GetRouteDatas(Route route, Address address)
        {
            // Start list of urls which could map to this item.  Note, catchall items (starting *) are treated as normal ones
            List<RouteValueDictionary> rvds = new List<RouteValueDictionary>();

            var keyOutputs = route.KeyOutputs();

            Type type = address.Type;

            ICollator collator = Collator.Instance.Registered(type);
            var rvd = new RouteValueDictionary(address);

            // Action & controller

            if (keyOutputs.ContainsKey("controller") && keyOutputs.ContainsKey("action"))
            {
                if (keyOutputs["controller"].StartsWith("?"))
                    throw new Exception("Route " + route.RouteTemplate + " is a data route which lacks but must have a specified controller");

                if (!ContentTypeHierarchy.ControllerActions.ContainsKey(keyOutputs["controller"]))
                    return new List<RouteData>();  // Controller doesn't exist therefore no rvds

                rvd.Add("controller", keyOutputs["controller"]);

                rvds.Add(rvd);

                // copy list so we can change 'actions'
                var actions = ContentTypeHierarchy.ControllerActions[keyOutputs["controller"]].ToList();
                if (keyOutputs["action"].StartsWith("?"))
                {
                    if (keyOutputs["action"].Length > 1 && actions.Contains(keyOutputs["action"].Substring(1).ToLower()))
                        actions.Add("??");
                    rvds = PermuteAdd(rvds, "action", actions);
                }
                else if (!actions.Contains(keyOutputs["action"]))
                    return new List<RouteData>(); // Fixed action doesn't exist therefore no urls
                else
                    rvd.Add("action", keyOutputs["action"]);
            }

            // Route vars addressing the content item

            foreach (var kvp in keyOutputs)
            {
                if (kvp.Key == "controller" || kvp.Key == "action") // already dealt with
                    continue;

                if (kvp.Value == "??" && address.ContainsKey(kvp.Key)) // optional variable
                {
                    address.SetMatched(kvp.Key);
                }
                else if (kvp.Value.StartsWith("?")) // variable takes any value
                {
                    // matches a path element
                    address.SetMatched(kvp.Key);   // this address element is matched
                }
                else // fixed value, has to be matched by item
                {
                    bool matched = false;
                    if (address.ContainsKey(kvp.Key))
                    {
                        if (address.GetAsString(kvp.Key) == kvp.Value)
                        {
                            matched = true;
                            address.SetMatched(kvp.Key);
                        }
                    }
                    if (!matched)
                        return new List<RouteData>();
                }
            }

            if (!address.FullyMatched) // Fails because one or more address elements could not have come from this route
                return new List<RouteData>();

            return rvds
                .Select(r =>
                    {
                        var rd = new RouteData();
                        r.Do(kvp => rd.Values.Add(kvp.Key, kvp.Value));
                        rd.Routers.Add(route);
                        return rd;
                    })
                .ToList();
        }

        /// <summary>
        /// Get distinct url patterns (with differing url arguments and therefore addressing different content items)
        /// </summary>
        /// <param name="type">The type for which to get the url patterns</param>
        /// <returns>The list of url patterns</returns>
        public IEnumerable<string> GetUrlPatterns(Type type)
        {
            var routes = new Route[0];
                //RouteTable.Routes
                //.OfType<Route>()
                //.Where(r => r.GetType().IsGenericType
                //    && r.GetType().GetGenericTypeDefinition() == typeof(DataRoute<>)
                //    && r.GetType().GetGenericArguments()[0] == type)
                //.OrderBy(r => r.Url.Length)
                //.ToArray();

            var routeArgs = routes.Select(r => r.KeyOutputs().Keys.OrderBy(k => k).Join("|")).ToArray();

            var urlPatterns = new List<string>();

            for (int i = 0; i < routes.Length; i++)
            {
                bool dupArgs = false;
                for (int j = 0; j < i; j++)
                {
                    if (routeArgs[j] == routeArgs[i])
                    {
                        dupArgs = true;
                        break;
                    }
                }
                if (dupArgs)
                    continue;
                
                urlPatterns.Add(routes[i].GetUrlPattern(null));
            }

            urlPatterns.RemoveAll(up => up == null);

            return urlPatterns;
        }

        /// <summary>
        /// Test whether any page exists at a content address's url
        /// </summary>
        /// <param name="address">Content address</param>
        /// <returns>True if a page exists there</returns>

        public bool AddressOccupied(Address address)
        {
            var urls = ContentMap.Instance
                .GetUrls(address);
            RouteData existingRd = urls
                .Select(url => url.Replace("??", "_").Replace("?", "_"))
                .Select(url => RouteX.GetRouteDataByUrl(url + "?$mode=bypass")) // $mode=bypass means the request is never diverted to an editor
                .FirstOrDefault(rd => !RouteIsNotOccupied(rd));
            return existingRd != null && existingRd.Routers.Count > 0;
        }
    }

}
