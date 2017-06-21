using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Models;
using Lynicon.Repositories;
using Lynicon.Linq;
using Lynicon.Utility;
using Linq2Rest;
using System.Collections.Specialized;
using Microsoft.AspNetCore.Routing;
using Lynicon.Extensibility;
using Microsoft.Extensions.Primitives;
using System.Reflection;
using LyniconANC.Extensibility;
using Lynicon.Services;
using Lynicon.Attributes;
using Newtonsoft.Json.Linq;

namespace Lynicon.Collation
{
    /// <summary>
    /// A class from which Collators must inherit which captures the common functionalities of
    /// all collators
    /// </summary>
    public abstract class BaseCollator : ICollator
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(BaseCollator));

        #region ICollator Members

        /// <summary>
        /// The repository to be used by this collator
        /// </summary>
        public Repository Repository { get { return System.Repository; } }

        public TypeExtender Extender { get { return System.Extender; } }

        public LyniconSystem System { get; set; }

        /// <summary>
        /// The container type this repository uses (or null if its just the content type)
        /// </summary>
        public abstract Type AssociatedContainerType { get; }

        public BaseCollator(LyniconSystem sys)
        {
            System = sys;
        }

        public abstract void BuildForTypes(IEnumerable<Type> types);

        /// <summary>
        /// Get data items via a list of data addresses
        /// </summary>
        /// <typeparam name="T">type of the items</typeparam>
        /// <param name="addresses">data addresses of the items</param>
        /// <returns>list of data items</returns>
        public abstract IEnumerable<T> Get<T>(IEnumerable<Address> a) where T : class;
        /// <summary>
        /// Get data items via a list of data addresses
        /// </summary>
        /// <typeparam name="T">type of the items</typeparam>
        /// <param name="addresses">data addresses of the items</param>
        /// <returns>list of data items</returns>
        public abstract IEnumerable<T> Get<T>(IEnumerable<ItemId> ids) where T : class;
        /// <summary>
        /// Get items via a query
        /// </summary>
        /// <typeparam name="T">return type, this can be a summmary type, a content type, or a class from which several content types inherit</typeparam>
        /// <typeparam name="TQuery">the type in terms of which the query is expressed: the content type or possibly a class from which several content types inherit</typeparam>
        /// <param name="types">a list of content types across which the query will be applied</param>
        /// <param name="queryBody">a function which takes an iqueryable and adds the query to the end of it</param>
        /// <returns>list of items of (or cast to) return type</returns>
        public abstract IEnumerable<T> Get<T, TQuery>(IEnumerable<Type> types, Func<IQueryable<TQuery>, IQueryable<TQuery>> queryBody)
            where T : class
            where TQuery : class;

        /// <summary>
        /// Gets a paged list of content items filtered by the OData $filter parameters present in the
        /// request represented by the rd (RouteData) parameter.
        /// </summary>
        /// <typeparam name="T">Returned element type of enumerable</typeparam>
        /// <typeparam name="TQuery">The type in which the $filter parameters are expressed</typeparam>
        /// <param name="types">The list of content types against which the filter is run</param>
        /// <param name="rd">The route data of the current request</param>
        /// <returns>Enumerable of type T of content items filtered and paged</returns>
        public virtual IEnumerable<T> GetList<T, TQuery>(IEnumerable<Type> types, RouteData rd)
            where T : class
            where TQuery : class
        {
            var parms = new NameValueCollection();
            RequestContextManager.Instance.CurrentContext.Request.Query
                .Do(kvp => parms.Add(kvp.Key, kvp.Value.FirstOrDefault()));
            if (rd.DataTokens.ContainsKey("top") && parms["$top"] == null)
            {
                parms["$top"] = new StringValues((string)rd.DataTokens["top"]);
            }
            if (rd.DataTokens.ContainsKey("orderBy") && parms["$orderBy"] == null)
            {
                parms["$orderBy"] = (string)rd.DataTokens["orderBy"];
            }

            if (parms["$orderBy"] != null && typeof(TQuery).GetProperty(parms["$orderBy"]) == null)
                parms.Remove("$orderBy");

            Func<IQueryable<TQuery>, IQueryable<TQuery>> queryBody = (iq => iq.Filter(parms).AsFacade<TQuery>());
            var parmsCount = new NameValueCollection(parms);
            parmsCount.Remove("$skip");
            parmsCount.Remove("$top");
            parmsCount.Remove("$orderBy");
            Func<IQueryable<TQuery>, IQueryable<TQuery>> queryBodyCount = (iq => iq.Filter(parmsCount).AsFacade<TQuery>());
            var qry = new List<TQuery>().Filter(parmsCount);

            int count;
            bool querySummary = typeof(Summary).IsAssignableFrom(typeof(TQuery));
            if (querySummary)
                count = Get<T, TQuery>(types, queryBodyCount).Count();
            else
                count = GetCount<TQuery>(types, queryBodyCount);
            var pSpec = PagingSpec.Create(parms);
            pSpec.Total = count;
            rd.DataTokens.Add("@Paging", pSpec);
            return Get<T, TQuery>(types, queryBody).ToList();
        }

        protected virtual Func<IQueryable<TQuery>, IQueryable<TQuery>> GetQueryBody<TQuery>(NameValueCollection parms)
            where TQuery : class
        {
            return iq => iq.Filter(parms).AsFacade<TQuery>();
        }
            
        protected virtual int GetCount<TQuery>(IEnumerable<Type> types, Func<IQueryable<TQuery>, IQueryable<TQuery>> queryBodyCount)
            where TQuery : class
        {
            return Repository.GetCount<TQuery>(types, queryBodyCount);
        }
        /// <summary>
        /// Get new item whose address is given
        /// </summary>
        /// <param name="a">the address to create it at</param>
        /// <returns>the new item</returns>
        public abstract T GetNew<T>(Address a) where T : class;

        /// <summary>
        /// Save new or modified item to data store
        /// </summary>
        /// <param name="address">the data address of the item, can be null if not available</param>
        /// <param name="data">the item to save</param>
        /// <param name="setOptions">list of options for saving, some may be custom</param>
        /// <returns>true if new record created</returns>
        public abstract bool Set(Address a, object data, Dictionary<string, object> setOptions);

        /// <summary>
        /// Delete item from data store
        /// </summary>
        /// <param name="address">the data address of the item (can be null if it can be derived from the item to delete)</param>
        /// <param name="data">the item to delete</param>
        public abstract void Delete(Address a, object data, bool bypassChecks);

        /// <summary>
        /// Move the data address of an item within the data store
        /// </summary>
        /// <param name="contentType">the type of the item</param>
        /// <param name="rd">the route data from which to obtain the new address of the item</param>
        /// <param name="id">the id of the item</param>
        public abstract void MoveAddress(ItemId id, Address moveTo);

        /// <summary>
        /// Get the data address of an item from type and route
        /// </summary>
        /// <param name="type">the type of the item</param>
        /// <param name="rd">the route data from which to get the data address</param>
        /// <returns>the data address</returns>
        public abstract Address GetAddress(Type type, RouteData rd);
        /// <summary>
        /// Get the data address of a container or content item where this is determined by the item itself
        /// </summary>
        /// <param name="data">the container or content item</param>
        /// <returns>the data address</returns>
        public abstract Address GetAddress(object data);

        /// <summary>
        /// Get a specified type summary of a content item or container
        /// </summary>
        /// <typeparam name="T">the type of the summary</typeparam>
        /// <param name="item">item to get summary of</param>
        /// <returns>summary of item</returns>
        public abstract T GetSummary<T>(object item) where T : class;

        /// <summary>
        /// Get a container object containing a content item
        /// </summary>
        /// <param name="item">the content item</param>
        /// <returns>a container containing the content item</returns>
        public abstract object GetContainer(Address a, object o);

        /// <summary>
        /// Get the container type (the extended type) from the content type
        /// </summary>
        /// <param name="contentType">The content type</param>
        /// <returns>The container type</returns>
        public Type ContainerType(Type contentType)
        {
            // if ct is already extended, must pass it unchanged
            Type ct = typeof(IContentContainer).IsAssignableFrom(contentType) ? contentType : UnextendedContainerType(contentType);
            ct = Extender[ct] ?? ct;
            return ct;
        }

        protected abstract Type UnextendedContainerType(Type type);

        /// <summary>
        /// Get the Identity property for a container type
        /// </summary>
        /// <param name="t">The container type</param>
        /// <returns>PropertyInfo for the Identity property</returns>
        public abstract PropertyInfo GetIdProperty(Type t);

        #endregion

        /// <summary>
        /// Starting from a list of addresses and optionally (or only) the containers at those addresses, fetch
        /// any containers necessary and any other containers required to supply redirected properties for them,
        /// obtain the contained content items and collate their properties, returning the content items at the
        /// addresses.
        /// </summary>
        /// <typeparam name="T">Type of content items to return</typeparam>
        /// <param name="startContainers">Initial list of containers if they are available</param>
        /// <param name="startAddresses">Initial list of addresses, which may be omitted and derived from containers</param>
        /// <returns>List of content items</returns>
        public IEnumerable<T> Collate<T>(IEnumerable<object> startContainers, IEnumerable<Address> startAddresses) where T : class
        {
            // place to store all the containers we have currently
            Dictionary<VersionedAddress, object> containers;
            ItemVersion containerCommonVersion;

            startAddresses = startAddresses ?? Enumerable.Empty<Address>();

            (containers, containerCommonVersion) = ProcessContainers(startContainers);

            List<Address> fetchAddrs = startAddresses
                .Where(sa => !containers.Any(kvp => kvp.Key.Address == sa)).ToList();

            var allStartAddressesByType = fetchAddrs.Concat(containers.Keys)
                .GroupBy(a => a.Type)
                .Select(ag => new { aType = ag.Key, addrs = ag.ToList() })
                .ToList();

            // Get all addresses for items to collate (startAddresses plus addresses from startContainers)
            foreach (var addrTypeG in allStartAddressesByType)
            {
                Type contentType = addrTypeG.aType;
                var rpsAttributes = contentType
                    .GetCustomAttributes(typeof(RedirectPropertySourceAttribute), false)
                    .Cast<RedirectPropertySourceAttribute>()
                    .ToList();
                foreach (Address addr in addrTypeG.addrs)
                {
                    fetchAddrs.AddRange(rpsAttributes
                        .Select(attr => new Address(attr.ContentType ?? contentType,
                            PathFunctions.Redirect(addr.GetAsContentPath(), attr.SourceDescriptor))));
                }
            }
            fetchAddrs = fetchAddrs.Distinct().ToList();

            bool pushVersion = (startContainers != null);
            if (pushVersion) // Get containers in any version that might be relevant to a start container
                System.Versions.PushState(VersioningMode.Specific, containerCommonVersion);

            try
            {
                // Get all the containers for collation (if current version is not fully specified, may be multiple per address)
                foreach (var cont in System.Repository.Get(typeof(object), fetchAddrs))
                {
                    var va = new VersionedAddress(System, cont);
                    if (containers.ContainsKey(va))
                        log.Error("Duplicate versioned address in db: " + va.ToString());
                    else
                        containers.Add(new VersionedAddress(new Address(cont), new ItemVersion(System, cont).Canonicalise()), cont); 
                }
            }
            finally
            {
                if (pushVersion)
                    System.Versions.PopState();
            }

            // Create a lookup by (non-versioned) address of all the containers we have
            var contLookup = containers.ToLookup(kvp => kvp.Key.Address.ToString(), kvp => kvp.Value);

            // We have the data, now collate it into the content from the startContainers
            foreach (var addrTypeG in allStartAddressesByType)
            {
                // Process all the start addresses (including those of the start containers) of a given type

                Type contentType = addrTypeG.aType;
                var rpsAttributes = contentType
                    .GetCustomAttributes(typeof(RedirectPropertySourceAttribute), false)
                    .Cast<RedirectPropertySourceAttribute>()
                    .ToList();

                foreach (var addrOrVAddr in addrTypeG.addrs)
                {
                    var addr = new Address(addrOrVAddr.Type, addrOrVAddr); // convert a VersionedAddress to an Address if necessary
                    var primaryPath = addr.GetAsContentPath();
                    if (!contLookup.Contains(new Address(addr.Type, addr).ToString()))
                        continue;

                    foreach (var cont in contLookup[addr.ToString()])
                    {
                        object primaryContent = cont;
                        JObject jContent = null;

                        if (primaryContent is IContentContainer)
                            primaryContent = ((IContentContainer)primaryContent).GetContent(System.Extender);
                        //jContent = JObject.FromObject(primaryContent);

                        foreach (var rpsAttribute in rpsAttributes)
                        {
                            var refAddress = new VersionedAddress(
                                rpsAttribute.ContentType ?? contentType,
                                PathFunctions.Redirect(primaryPath, rpsAttribute.SourceDescriptor),
                                new ItemVersion(System, cont).Canonicalise()
                                );
                            if (refAddress.Address == addr) // redirected to itself, ignore
                                continue;
                            object refItem = containers.ContainsKey(refAddress) ? containers[refAddress] : null;
                            if (refItem is IContentContainer)
                                refItem = ((IContentContainer)refItem).GetContent(System.Extender);
                            if (refItem != null)
                                foreach (string propertyPath in rpsAttribute.PropertyPaths)
                                {
                                    var toFromPaths = GetPaths(propertyPath);
                                    //JObject refdObject = JObject.FromObject(refItem);
                                    //jContent.CopyPropertyFrom(toFromPaths[0], refdObject, toFromPaths[1]);
                                    object val = ReflectionX.GetPropertyValueByPath(refItem, toFromPaths[1]);
                                    var piSet = ReflectionX.GetPropertyByPath(primaryContent.GetType(), toFromPaths[0]);
                                    piSet.SetValue(primaryContent, val);
                                }
                        }

                        //primaryContent = jContent.ToObject(primaryContent.GetType(), new JsonSerializer());
                        yield return primaryContent as T;
                    }
                }
            }
        }

        public (Dictionary<VersionedAddress, object>, ItemVersion) ProcessContainers(IEnumerable<object> startContainers)
        {
            var containers = new Dictionary<VersionedAddress, object>();
            ItemVersion containerCommonVersion = null;
            // Ensure we have the start addresses
            if (startContainers != null)
            {
                foreach (var cont in startContainers)
                {
                    var cVersAddr = new VersionedAddress(System, cont);
                    if (!containers.ContainsKey(cVersAddr))
                        containers.Add(cVersAddr, cont);
                    else
                        log.Error("Duplicate versioned address: " + cVersAddr.ToString());

                    containerCommonVersion = containerCommonVersion == null ? cVersAddr.Version : containerCommonVersion.LeastAbstractCommonVersion(cVersAddr.Version);
                }
            }

            return (containers, containerCommonVersion);
        }

        protected virtual string[] GetPaths(string path)
        {
            if (path.Contains(">"))
                return path.Split('>').Select(s => s.Trim()).ToArray(); // primary path > redirect path
            else
                return new string[] { path, path };
        }

        /// <summary>
        /// Decollates changes to content object which should be redirected to other records used as property sources
        /// </summary>
        /// <param name="path">path of content record</param>
        /// <param name="data">content object</param>
        /// <returns>JObject build from content object</returns>
        protected virtual object SetRelated(string path, object data, bool bypassChecks)
        {

            System.Versions.PushState(VersioningMode.Specific, new ItemVersion(System, data));

            try
            {
                JObject jObjectContent = JObject.FromObject(data);

                // Establish the records to fetch and fetch them

                Type contentType = data.GetType().UnextendedType();
                var rpsAttributes = contentType
                    .GetCustomAttributes(typeof(RedirectPropertySourceAttribute), false)
                    .Cast<RedirectPropertySourceAttribute>()
                    .Where(rpsa => !rpsa.ReadOnly)
                    .ToList();
                //List<string> paths = rpsAttributes
                //        .Select(a => PathFunctions.Redirect(path, a.SourceDescriptor))
                //        .Distinct()
                //        .ToList();
                //if (paths == null || paths.Count == 0)
                //    return jObjectContent;

                //List<ContentItem> records = Repository.GetByPath(contentType, paths).ToList();

                List<Address> addresses = rpsAttributes
                    .Select(a => new Address(a.ContentType ?? contentType, PathFunctions.Redirect(path, a.SourceDescriptor)))
                    .Distinct()
                    .ToList();
                if (addresses == null || addresses.Count == 0)
                    return data;
                List<object> records = System.Repository.Get(typeof(object), addresses).ToList();

                // Update the fetched referenced records with updated referenced properties on the content object

                List<Address> doneAddrs = new List<Address>();
                List<object> vals = new List<object>();
                var writebacks = new Dictionary<string[], object>();

                foreach (var rpsAttribute in rpsAttributes)
                {
                    Address address = new Address(
                        rpsAttribute.ContentType ?? contentType,
                        PathFunctions.Redirect(path, rpsAttribute.SourceDescriptor));

                    string refdPath = address.GetAsContentPath();
                    Type refdType = address.Type;

                    if (refdPath == path && refdType == contentType) // redirected to itself, ignore
                        continue;
                    object refdRecord = records.FirstOrDefault(r => new Address(r) == address);
                    object refdContent = refdRecord;
                    if (refdRecord is IContentContainer)
                        refdContent = ((IContentContainer)refdRecord).GetContent(System.Extender);
                    if (refdRecord == null) // adding a new record
                    {
                        refdContent = System.Collator.GetNew(address);
                        refdRecord = System.Collator.GetContainer(address, refdContent);
                    }

                    JObject refdObject = JObject.FromObject(refdContent);
                    List<string[]> writebackPaths = new List<string[]>();
                    foreach (string propertyPath in rpsAttribute.PropertyPaths)
                    {
                        var toFromPaths = GetPaths(propertyPath);
                        if (toFromPaths[0].EndsWith("<"))
                        {
                            toFromPaths[0] = toFromPaths[0].UpToLast("<");
                            toFromPaths[1] = toFromPaths[1].UpToLast("<");
                            writebackPaths.Add(toFromPaths);
                        }
                        refdObject.CopyPropertyFrom(toFromPaths[1], jObjectContent, toFromPaths[0]);
                    }

                    if (refdRecord is IContentContainer)
                    {
                        Type valType = ((IContentContainer)refdRecord).ContentType;
                        valType = System.Extender[valType] ?? valType;
                        ((IContentContainer)refdRecord).SetContent(System, refdObject.ToObject(valType));
                    }
                    else
                        refdRecord = refdObject.ToObject(refdRecord.GetType());

                    if (!doneAddrs.Contains(address))
                    {
                        doneAddrs.Add(address);
                        vals.Add(refdRecord);
                    }

                    writebackPaths.Do(wp => writebacks.Add(wp, refdRecord));
                }

                // Create or update referred-to records
                if (vals.Count > 0)
                {
                    Repository.Set(vals, null, bypassChecks);

                    // write back any values configured by attributes (e.g. database index updates)
                    foreach (var kvp in writebacks)
                    {
                        JObject refdObject = JObject.FromObject(kvp.Value);
                        jObjectContent.CopyPropertyFrom(kvp.Key[0], refdObject, kvp.Key[1]);
                    }
                    data = jObjectContent.ToObject(data.GetType());
                }

                return writebacks.Count > 0 ? data : null;
            }
            finally
            {
                System.Versions.PopState();
            }
        }

    }
}
