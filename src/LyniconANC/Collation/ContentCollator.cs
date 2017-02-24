
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Utility;
using Lynicon.Repositories;
using Lynicon.Extensibility;
using Lynicon.Models;
using Lynicon.Routing;
using Lynicon.Attributes;
using Newtonsoft.Json.Linq;
using Lynicon.Map;
using Lynicon.Linq;
using Linq2Rest;
using Lynicon.Relations;
using Microsoft.AspNetCore.Routing;
using LyniconANC.Exceptions;

namespace Lynicon.Collation
{
    /// <summary>
    /// Collator for the Content persistence model (which JSON encodes content data into summary and content parts in a SQL table,
    /// storing metadata in the other SQL table fields)
    /// </summary>
    public class ContentCollator : BaseCollator, ICollator
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ContentCollator));

        public ContentCollator(Repository repository)
        {
            this.Repository = repository;
        }

        /// <inheritdoc/>
        public override Type AssociatedContainerType { get { return typeof(ContentItem); } }

        /// <inheritdoc/>
        public override IEnumerable<T> Get<T>(IEnumerable<Address> addresses)
        {
            if (typeof(Summary).IsAssignableFrom(typeof(T)))
            {
                foreach (var ag in addresses.GroupBy(a => a.Type))
                {
                    var conts = Repository.Get<ContentItem>(typeof(T), ag);
                    foreach (var cont in conts)
                    {
                        var summ = cont.GetSummary();
                        if (summ is T)
                            yield return summ as T;
                    }
                    //foreach (var res in Repository.GetByPath(typeof(T), ag.Select(a => a.GetAsContentPath()).ToList()))
                    //{
                    //    var summ = res.GetSummary();
                    //    if (summ is T)
                    //        yield return res.GetSummary() as T;
                    //}
                }

            }
            else
            {
                //foreach (var res in addresses.Select(a => GetWithRelated(a.Type, a.GetAsContentPath(), null)))
                //    yield return (T)res;
                foreach (var res in Collate<T>(null, addresses))
                    yield return res;
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<T> Get<T>(IEnumerable<ItemId> ids)
        {
            if (typeof(Summary).IsAssignableFrom(typeof(T)))
                return Repository.Get<ContentItem>(typeof(T), ids)
                        .Select(ci => ci.GetSummary() as T)
                        .Where(s => s != null);
            else
                //return Repository.Get<ContentItem>(typeof(T), ids)
                //        .Select(ci => ci.GetContent<T>());
                return Collate<T>(Repository.Get<ContentItem>(typeof(T), ids), null);
        }

        /// <inheritdoc/>
        public override IEnumerable<T> Get<T, TQuery>(IEnumerable<Type> types, Func<IQueryable<TQuery>, IQueryable<TQuery>> queryBody)
        {
            bool isSummary = typeof(Summary).IsAssignableFrom(typeof(T));
            bool querySummary = typeof(Summary).IsAssignableFrom(typeof(TQuery));

            var dummy = new TQuery[0].AsQueryable();
            IQueryable<TQuery> results;
            bool isContainerQuery = typeof(TQuery).ContentType() == typeof(ContentItem)
                || (typeof(TQuery).IsAssignableFrom(typeof(ContentItem))
                    && queryBody(dummy).ExtractFields().All(fn => typeof(ContentItem).GetProperty(fn) != null));
            if (isContainerQuery)
            {
                Func<IQueryable<ContentItem>, IQueryable<ContentItem>> containerQueryBody = iq => queryBody(iq.AsFacade<TQuery>()).AsFacade<ContentItem>();
                results = Repository
                    .Get<ContentItem>(typeof(T), types, containerQueryBody)
                    .Cast<TQuery>()
                    .AsQueryable();
            }
            else if (isSummary && querySummary)
            {
                // Get all summaries from repository and filter them in memory

                results = Repository
                            .Get<ContentItem>(typeof(T), types, iq => iq)
                            .AsEnumerable()
                            .Select(ci => ci.GetSummary())
                            .OfType<TQuery>()
                            .AsQueryable();
                results = queryBody(results);
            }
            else // most inefficient choice, gets all items from repository then filters them in memory
                // needs work, bringing back an empty record if T is a summary because doesn't load content field
            {
                var preCollate = Repository
                .Get<ContentItem>(typeof(T), types, iq => iq)
                .AsEnumerable();

                var preResults = Collate<TQuery>(preCollate, null)
                .AsQueryable();

                // Apply query after all items of listed types have been pulled from database, potentially very inefficient
                results = queryBody(preResults);
            }
            
            if (isSummary && !querySummary)
            {
                foreach (var summ in results.Select(r => GetSummary<T>(r)))
                    yield return summ as T;
            }
            else if (isContainerQuery)
            {
                foreach (var item in Collate<T>(results.AsEnumerable(), null))
                    yield return item;
            }
            else
            {
                foreach (var item in results)
                    yield return item as T;
            }

        }

        /// <inheritdoc/>
        public override T GetNew<T>(Address a)
        {
            if (a == null)
                throw new ArgumentException("Trying to create a new item via ContentCollator but no way of generating an address, supply a GetDefaultAddress() method for " + typeof(T).FullName);
            string path = a.GetAsContentPath();
            ContentItem newRecord = GetNewRecord<T>(path);
            //return (T)GetWithRelated(typeof(T), path, newRecord);
            return Collate<T>(new object[] { newRecord }, new Address[] { a }).Single();
        }

        /// <inheritdoc/>
        public override TTarget GetSummary<TTarget>(object item)
        {
            return ContentItem.GetSummary(item) as TTarget;
        }

        //public override object Summarise(object item)
        //{
        //    ContentItem ci = (ContentItem)ReflectionX.CopyEntity(item);
        //    ci.Content = null;
        //    var dummy = ci.GetSummary(); // ensure summary is deserialised
        //    ci.Summary = null; 
        //    return ci;
        //}

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
            var containers = new Dictionary<VersionedAddress, object>();

            ItemVersion containerCommonVersion = null;
            // Ensure we have the start addresses
            if (startContainers != null)
            {
                var startAddrList = new List<Address>();
                foreach (var cont in startContainers)
                {
                    var cVersAddr = new VersionedAddress(cont);
                    if (!containers.ContainsKey(cVersAddr))
                    {
                        startAddrList.Add(cVersAddr.Address);
                        containers.Add(cVersAddr, cont);
                    }
                    else
                        log.Error("Duplicate versioned address: " + cVersAddr.ToString());

                    containerCommonVersion = containerCommonVersion == null ? cVersAddr.Version : containerCommonVersion.LeastAbstractCommonVersion(cVersAddr.Version);
                }
                startAddresses = startAddrList.Distinct();
            }

            List<Address> addrs = new List<Address>();
            var contAddrs = new HashSet<Address>(containers.Keys.Select(va => va.Address));
            // Get all addresses for items to collate
            foreach (var addrTypeG in startAddresses.GroupBy(a => a.Type))
            {
                Type contentType = addrTypeG.Key;
                var rpsAttributes = contentType
                    .GetCustomAttributes(typeof(RedirectPropertySourceAttribute), false)
                    .Cast<RedirectPropertySourceAttribute>()
                    .ToList();
                List<Address> collAddrs = new List<Address>();
                foreach (Address addr in addrTypeG)
                {
                    string path = addr.GetAsContentPath();
                    if (!contAddrs.Contains(addr))
                        addrs.Add(addr);
                    addrs.AddRange(rpsAttributes
                        .Select(a => new Address(a.ContentType ?? contentType,
                            PathFunctions.Redirect(path, a.SourceDescriptor))));
                }
            }
            addrs = addrs.Distinct().ToList();

            bool pushVersion = (startContainers != null);
            if (pushVersion) // Get containers in any version that might be relevant to a start container
                VersionManager.Instance.PushState(VersioningMode.Specific, containerCommonVersion);

            try
            {
                // Get all the containers for collation (if current version is not fully specified, may be multiple per address)
                foreach (var cont in Repository.Instance.Get(typeof(object), addrs))
                {
                    var va = new VersionedAddress(cont);
                    if (containers.ContainsKey(va))
                        log.Error("Duplicate versioned address in db: " + va.ToString());
                    else
                        containers.Add(new VersionedAddress(cont), cont);
                }
            }
            finally
            {
                if (pushVersion)
                    VersionManager.Instance.PopState();
            }

            var contLookup = containers.ToLookup(kvp => kvp.Key.Address.ToString(), kvp => kvp.Value);

            if (startContainers == null)
            {
                startContainers = startAddresses.SelectMany(a => contLookup[a.ToString()]);
            }

            // We have the data, now collate it into the content from the startContainers
            foreach (var addrTypeG in startAddresses.GroupBy(a => a.Type))
            {
                // Process all the start addresses of a given type

                Type contentType = addrTypeG.Key;
                var rpsAttributes = contentType
                    .GetCustomAttributes(typeof(RedirectPropertySourceAttribute), false)
                    .Cast<RedirectPropertySourceAttribute>()
                    .ToList();

                foreach (var addr in addrTypeG)
                {
                    var primaryPath = addr.GetAsContentPath();
                    if (!contLookup.Contains(addr.ToString()))
                        continue;

                    foreach (var cont in contLookup[addr.ToString()])
                    {
                        object primaryContent = cont;
                        JObject jContent = null;

                        if (primaryContent is IContentContainer)
                            primaryContent = ((IContentContainer)primaryContent).GetContent();
                        //jContent = JObject.FromObject(primaryContent);

                        foreach (var rpsAttribute in rpsAttributes)
                        {
                            var refAddress = new VersionedAddress(
                                rpsAttribute.ContentType ?? contentType,
                                PathFunctions.Redirect(primaryPath, rpsAttribute.SourceDescriptor),
                                new ItemVersion(cont)
                                );
                            if (refAddress.Address == addr) // redirected to itself, ignore
                                continue;
                            object refItem = containers.ContainsKey(refAddress) ? containers[refAddress] : null;
                            if (refItem is IContentContainer)
                                refItem = ((IContentContainer)refItem).GetContent();
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

        /// <inheritdoc/>
        public override bool Set(Address a, object data, Dictionary<string, object> setOptions)
        {
            if (a == null)
                a = GetAddress(data);

            var ci = (ContentItem)GetContainer(a, data);

            var updatedData = SetRelated(ci.Path, data, (bool)(setOptions.ContainsKey("bypassChecks") ? setOptions["bypassChecks"] : false));

            if (updatedData != null)
                ci = (ContentItem)GetContainer(a, updatedData);

            if (data is BaseContent && ((BaseContent)data).OriginalRecord == null)
                ((BaseContent)data).OriginalRecord = ci;

            var created = Repository.Set(new List<object> { ci }, setOptions);

            return created[0];
        }

        /// <inheritdoc/>
        public override void Delete(Address a, object data, bool bypassChecks)
        {
            if (a == null)
                a = GetAddress(data);

            var ci = (ContentItem)GetContainer(a, data);

            ci.SetContent(data);

            Repository.Delete(ci, bypassChecks);
        }

        /// <inheritdoc/>
        public override void MoveAddress(ItemId id, Address moveTo)
        {
            //var existingSumm = Repository.Get<ContentItem>(typeof(Summary), moveTo).FirstOrDefault();
            //if (existingSumm != null)
            //    throw new ApplicationException("There is an item already at that address");

            if (ContentMap.Instance.AddressOccupied(moveTo))
                throw new LyniconANC.Exceptions.ApplicationException("There is an item already at that address");

            var contentItem = Repository.Get<ContentItem>(id.Type, id.Id);

            // If the address is dependent on data fields, set those fields correspondingly
            object data = contentItem.GetContent();
            Address address = new Address(data);
            bool hasAddressFields = address.Count > 0;
            address = moveTo;
            if (hasAddressFields)
            {
                address.SetAddressFields(data);
                contentItem.SetContent(data);
            }

            contentItem.Path = address.GetAsContentPath();

            EventHub.Instance.ProcessEvent("Content.Move", this, Tuple.Create(moveTo, contentItem));

            Repository.Set(contentItem);
        }

        private ContentItem GetContentItem(Address a, object data)
        {
            string dataPath = null;
            string routePath = null;
            ContentItem contentItem = null;

            // try and get path from data, or else from rvdict
            Address address = new Address(data);
            if (address.Count > 0)
                dataPath = address.GetAsContentPath();

            if (a != null)
            {
                routePath = a.GetAsContentPath();
            }

            if (dataPath != null && routePath != null && dataPath != routePath)
            {
                // Raise event here for when address is changed via changing addressed mapped fields on data
                EventHub.Instance.ProcessEvent("Content.Move", this, Tuple.Create(a, data));
                // regenerate path in case event processor changed data
                address = new Address(data);
                dataPath = address.GetAsContentPath();
                routePath = a.GetAsContentPath();
            }

            string path = dataPath ?? routePath;

            // if we have a BaseContent, we can use the OriginalRecord if must as we have no path, or if the path is the same
            if (data is BaseContent)
            {
                contentItem = ((BaseContent)data).OriginalRecord;
                if (contentItem != null && (path == null || path == contentItem.Path))
                {
                    contentItem.SetContent(data);
                    return contentItem;
                }
            }

            // If we get to here, we can't find the path
            if (path == null)
                throw new ArgumentException("Cannot find path of " + data.GetType().FullName);

            // Now we have to get the content item from the db so we get the right ids etc
            var findPath = routePath ?? dataPath;
            var contentItems = Repository.Get<ContentItem>(data.GetType(), iq => iq.Where(ci => ci.Path == findPath)).ToList();
            if (contentItems.Count > 1)
                throw new Exception("Duplicate content items at " + findPath + " of type " + data.GetType().FullName);

            contentItem = contentItems.SingleOrDefault();
            // If we can't we build a new one
            if (contentItem == null)
            {
                contentItem = Repository.New<ContentItem>();
                contentItem.DataType = data.GetType().FullName;
            }

            contentItem.Path = path;

            contentItem.SetContent(data);

            if (data is BaseContent)
                ((BaseContent)data).OriginalRecord = contentItem;

            return contentItem;
        }

        /// <summary>
        /// Decollates changes to content object which should be redirected to other records used as property sources
        /// </summary>
        /// <param name="path">path of content record</param>
        /// <param name="data">content object</param>
        /// <returns>JObject build from content object</returns>
        protected virtual object SetRelated(string path, object data, bool bypassChecks)
        {

            VersionManager.Instance.PushState(VersioningMode.Specific, new ItemVersion(data));

            try
            {
                JObject jObjectContent = JObject.FromObject(data);

                // Establish the records to fetch and fetch them

                Type contentType = data.GetType();
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
                List<object> records = Repository.Instance.Get(typeof(object), addresses).ToList();

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
                        refdContent = ((IContentContainer)refdRecord).GetContent();
                    if (refdRecord == null) // adding a new record
                    {
                        refdContent = Collator.Instance.GetNew(address);
                        refdRecord = Collator.Instance.GetContainer(address, refdContent);
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
                        ((IContentContainer)refdRecord).SetContent(refdObject.ToObject(((IContentContainer)refdRecord).ContentType));
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
                VersionManager.Instance.PopState();
            }
        }

        protected virtual string[] GetPaths(string path)
        {
            if (path.Contains(">"))
                return path.Split('>').Select(s => s.Trim()).ToArray(); // primary path > redirect path
            else
                return new string[] { path, path };
        }

        protected ContentItem GetNewRecord<T>(string path)
        {
            return GetNewRecord(typeof(T), path);
        }
        protected virtual ContentItem GetNewRecord(Type type, string path)
        {
            var newCI = Repository.New<ContentItem>();
            newCI.Path = path;
            newCI.DataType = type.FullName;
            var newContent = Activator.CreateInstance(type);
            var address = new Address(type, path);
            address.SetAddressFields(newContent);
            newContent = EventHub.Instance.ProcessEvent("ContentItem.New", this, newContent).Data;
            if (newContent is BaseContent)
                ((BaseContent)newContent).OriginalRecord = newCI;
            newCI.SetContent(newContent);
            // ensure it is created in the current version
            VersionManager.Instance.SetVersion(VersionManager.Instance.CurrentVersion, newCI);
            return newCI;
        }

        /// <inheritdoc/>
        public override Address GetAddress(Type type, RouteData rd)
        {
            Address address = new Address();
            int ord;
            rd.Values
                .Where(v => (v.Key.StartsWith("_")
                            && int.TryParse(v.Key.After("_").UpTo("-"), out ord)
                            && (v.Value ?? "").ToString() != ""))
                .Do(kvp => address.Add(kvp.Key.UpTo("-").Replace("*", ""), kvp.Value ?? ""));
            address.Type = type;
            address.FixCase();
            return address;
        }
        /// <inheritdoc/>
        public override Address GetAddress(object o)
        {
            // try to get address from fields on item marked with AddressComponentAttribute
            Address address = new Address();
            if (!(o is ContentItem))
                address = new Address(o);

            // if no such field, try saved path
            if (address.Count == 0 && (o is ContentItem || o is BaseContent))
            {
                string path = null;
                if (o is ContentItem)
                {
                    path = ((ContentItem)o).Path;
                    if (path == null)
                        return null;
                    else
                        address = new Address(((ContentItem)o).ContentType, path);
                }
                else
                {
                    path = ((BaseContent)o).OriginalRecord.Path;
                    if (path == null)
                        return null;
                    else
                        address = new Address(o.GetType(), path);
                }
            }

            address.FixCase();
            return address;
        }

        /// <inheritdoc/>
        public override object GetContainer(Address a, object o)
        {
            var ci = GetContentItem(a, o);
            var data = new Dictionary<string, object> { { "Item", o }, { "Container", ci } };
            ci = EventHub.Instance.ProcessEvent("Collator.GetContainer", this, data).GetDataItem<ContentItem>("Container");
            return ci;
        }

        protected override Type UnextendedContainerType(Type type)
        {
            return typeof(ContentItem);
        }

        /// <inheritdoc/>
        public override PropertyInfo GetIdProperty(Type t)
        {
            return t.GetProperty("Identity");
        }
    }
}
