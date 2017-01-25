using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Membership;
using Lynicon.Models;
using Lynicon.Utility;
using Lynicon.Attributes;
using Lynicon.DataSources;

namespace Lynicon.Repositories
{
    /// <summary>
    /// Possible lifetimes for the db context
    /// </summary>
    public enum ContextLifetimeMode
    {
        PerCall,
        PerRequest
    }

    /// <summary>
    /// The front end of all repository accesses
    /// </summary>
    public class Repository : TypeRegistry<IRepository>, IRepository
    {
        static readonly Repository instance = new Repository();
        public static Repository Instance { get { return instance; } }

        static Repository() { }


        #region IRepository Members

        public IDataSourceFactory DataSourceFactory { get { return null; } }

        public Repository()
        {
            this.DefaultHandler = new ContentRepository(this, new CoreDataSourceFactory());
            this.NoTypeProxyingInScope = true;
            this.QueryTimeoutSecs = null;
            this.AvoidConnection = false;
        }

        const string NoTypeProxyingKey = "lyn_notypeproxying";
        /// <summary>
        /// Whether repositories are allowed to return dynamic proxies for requested content types
        /// in the current thread/request scope
        /// </summary>
        public bool NoTypeProxyingInScope
        {
            get
            {
                return RequestThreadCache.Current.ContainsKey(NoTypeProxyingKey)
                    ? (bool)RequestThreadCache.Current[NoTypeProxyingKey]
                    : false;
            }
            set
            {
                RequestThreadCache.Current[NoTypeProxyingKey] = value;
            }
        }

        /// <summary>
        /// Stops the repository connecting to the data source, allowing all data to be
        /// source and stored in a full cache
        /// </summary>
        public bool AvoidConnection { get; set; }

        public int? QueryTimeoutSecs { get; set; }

        /// <summary>
        /// Set up repository not to use a data source
        /// </summary>
        public void EnsureNoDatabase()
        {
            NoTypeProxyingInScope = false;
            AvoidConnection = true;
        }


        /// <summary>
        /// Generate a new instance of type T via the data API
        /// </summary>
        /// <typeparam name="T">type of object to create</typeparam>
        /// <returns>initialised object</returns>
        public T New<T>() where T : class
        {
            return New(typeof(T)) as T;
        }
        /// <summary>
        /// Generate a new instance of type t via the data API
        /// </summary>
        /// <param name="t">type of object to create</param>
        /// <returns>initialised object</returns>
        public object New(Type t)
        {
            object newContainer = Registered(t).New(t);
            if (newContainer is IBasicAuditable)
            {
                IBasicAuditable aud = (IBasicAuditable)newContainer;
                aud.Created = aud.Updated = DateTime.UtcNow;
                aud.UserCreated = aud.UserUpdated = (SecurityManager.Current?.UserId ?? "");
            }
            return newContainer;
        }

        /// <summary>
        /// Get an item by id
        /// </summary>
        /// <typeparam name="T">type returned by repository</typeparam>
        /// <param name="targetType">type of data item this will produce (the content type, not a summary type)</param>
        /// <param name="id">id of item</param>
        /// <returns>item in a container or the item itself</returns>
        public T Get<T>(Type targetType, object id) where T : class
        {
            return Get<T>(targetType, new List<ItemId> { new ItemId { Id = id, Type = targetType } }).FirstOrDefault();
        }
        /// <summary>
        /// Get an item's summary by id
        /// </summary>
        /// <typeparam name="T">type returned by repository</typeparam>
        /// <param name="summaryType">type of summary this will produce</param>
        /// <param name="targetType">the type of which this is a summary</param>
        /// <param name="id">id of item</param>
        /// <returns>summary details of item in a container</returns>
        public T Get<T>(Type summaryType, Type targetType, object id) where T : class
        {
            return Get<T>(summaryType, new List<ItemId> { new ItemId { Id = id, Type = targetType } }).FirstOrDefault();
        }
        /// <summary>
        /// Get an item by id
        /// </summary>
        /// <param name="returnType">type returned by repository</param>
        /// <param name="targetType">type of data item this will produce (not a summary type)</param>
        /// <param name="id">id of item</param>
        /// <returns>item in a container or the item itself</returns>
        public object Get(Type returnType, Type targetType, object id)
        {
            return ReflectionX.InvokeGenericMethod(this, "Get", returnType, targetType, id);
        }
        /// <summary>
        /// Get an item's summary by id
        /// </summary>
        /// <param name="returnType">type returned by repository</param>
        /// <param name="summaryType">type of summary this will produce</param>
        /// <param name="targetType">the type of which this is a summary</param>
        /// <param name="id">id of item</param>
        /// <returns>summary details of item in a container</returns>
        public object Get(Type returnType, Type summaryType, Type targetType, object id)
        {
            return ReflectionX.InvokeGenericMethod(this, "Get", returnType, summaryType, targetType, id);
        }
        /// <summary>
        /// Get some items by their ids
        /// </summary>
        /// <typeparam name="T">type of items returned by repository</typeparam>
        /// <param name="targetType">type of items these will produce (can be a summary type)</param>
        /// <param name="ids">ids of items</param>
        /// <returns>items in containers or the items themselves</returns>
        public IEnumerable<T> Get<T>(IEnumerable<ItemId> ids) where T : class
        {
            return Get<T>(typeof(object), ids);
        }
        /// <summary>
        /// Get some items by their ids
        /// </summary>
        /// <typeparam name="T">type of items returned by repository</typeparam>
        /// <param name="targetType">type of items these will produce</param>
        /// <param name="ids">ids of items</param>
        /// <returns>items in containers or the items themselves</returns>
        public IEnumerable<T> Get<T>(Type targetType, IEnumerable<ItemId> ids) where T : class
        {
            // handle ItemIds
            foreach (var idg in ids.Where(id => id.GetType() == typeof(ItemId)).OfType<ItemId>().GroupBy(ii => Registered(ii.Type)))
                foreach (var res in idg.Key.Get<T>(targetType, idg))
                    yield return res;

            // get versioned ids in list, group by version and fix version manager to that version

            var versionGroups = ids.OfType<ItemVersionedId>().GroupBy(ivi => ivi.Version).ToList(); // may want to know current version for unassigned ivid versions
            VersionManager.Instance.PushState(VersioningMode.Specific);
            try
            {
                foreach (var idGp in versionGroups)
                {
                    VersionManager.Instance.SpecificVersion = idGp.Key;
                    foreach (var idTg in idGp.GroupBy(ivi => Registered(ivi.Type)))
                        foreach (var res in idTg.Key.Get<T>(targetType, idGp))
                            yield return res;
                }
            }
            finally
            {
                VersionManager.Instance.PopState();
            }

        }
        /// <summary>
        /// Get some items by their ids
        /// </summary>
        /// <param name="returnType">type of items returned by repository</param>
        /// <param name="targetType">type of items these will produce</param>
        /// <param name="ids">ids of items</param>
        /// <returns>items in containers or the items themselves</returns>
        public IEnumerable<object> Get(Type returnType, Type targetType, IEnumerable<object> ids)
        {
            return (IEnumerable<object>)ReflectionX.InvokeGenericMethod(this, "Get", mi => mi.GetParameters()[2].ParameterType == typeof(IEnumerable<object>), new Type[] { returnType }, targetType, targetType, ids);
        }
        /// <summary>
        /// Get some summaries by their ids
        /// </summary>
        /// <param name="returnType">type of items returned by repository</param>
        /// <param name="summaryType">type of summary these will produce</param>
        /// <param name="targetType">the type of which these are summaries</param>
        /// <param name="ids">ids of items</param>
        /// <returns>summary details of items in containers</returns>
        public IEnumerable<object> Get(Type returnType, Type summaryType, Type targetType, IEnumerable<object> ids)
        {
            return (IEnumerable<object>)ReflectionX.InvokeGenericMethod(this, "Get", mi => mi.GetParameters()[2].ParameterType == typeof(IEnumerable<object>), new Type[] { returnType }, summaryType, targetType, ids);
        }
        /// <summary>
        /// Get containers by addresses
        /// </summary>
        /// <param name="targetType">type of contained items these will produce - either 'object' or a summary type</param>
        /// <param name="addresses">list of addresses to fetch</param>
        /// <returns>container(s) at addresses</returns>
        public IEnumerable<object> Get(Type targetType, IEnumerable<Address> addresses)
        {
            foreach (var addressG in addresses.GroupBy(ad => ad.Type))
            {
                Type contT = Collator.Instance.ContainerType(addressG.Key);
                var pathPiInfo = contT.GetProperties()
                    .Select(pi => new {pi, a = pi.GetCustomAttribute<AddressComponentAttribute>()})
                    .FirstOrDefault(pii => pii.a != null && pii.a.UsePath);
                if (pathPiInfo != null) // container has a single property in which the path is stored - we can get all using same repository in one query
                {
                    foreach (object res in (IEnumerable)ReflectionX.InvokeGenericMethod(this, "Get",
                                                mi => { var parms = mi.GetParameters(); return parms.Length == 2 && parms[1].Name == "addresses"; },
                                                new Type[] { contT }, targetType, addressG))
                        yield return res;
                }
                else // we have to use a query for each item as the path is spread across multiple fields
                {
                    foreach (Address a in addressG)
                    {
                        var results = (IEnumerable)ReflectionX.InvokeGenericMethod(this, "Get", m => m.GetParameters()[1].ParameterType == typeof(Address), new Type[] { contT }, a.Type, a);
                        foreach (var res in results)
                            yield return res;
                    }
                }

            }
        }
        /// <summary>
        /// Get containers of a specific type by addresses 
        /// </summary>
        /// <typeparam name="T">The type of containers to get</typeparam>
        /// <param name="targetType">type of contained items these will produce, can be a summary type</param>
        /// <param name="addresses">list of addresses to fetch (ignored if don't have the container type specified)</param>
        /// <returns>containers at addresses</returns>
        public IEnumerable<T> Get<T>(Type targetType, IEnumerable<Address> addresses) where T : class
        {
            if (!addresses.Any())
                yield break;

            var contentType = addresses.First().Type;
            var paths = addresses.Where(
                a => typeof(T).IsAssignableFrom(Collator.Instance.ContainerType(a.Type)) && a.Type == contentType)
                .Select(a => a.GetAsContentPath()).ToList();
            var pathPiInfo = typeof(T).GetProperties()
                    .Select(pi => new { pi, a = pi.GetCustomAttribute<AddressComponentAttribute>() })
                    .FirstOrDefault(pii => pii.a != null && pii.a.UsePath);
            var pathSel = LinqX.GetFieldSelector<T>(pathPiInfo.pi.Name);
            var repo = Registered(typeof(T));
            foreach (var res in repo.Get<T>(targetType, new Type[] { contentType }, iq => iq.WhereIn(pathSel, paths)))
                yield return res;
        }
        /// <summary>
        /// Get container(s) by an address
        /// </summary>
        /// <typeparam name="T">type of container(s) returned by repository</typeparam>
        /// <param name="targetType">type of contained items these will produce - this is the type of the address, or the summary type of that type</param>
        /// <param name="address">address of item (or different versions of the item) to fetch</param>
        /// <returns>container(s) at the address specified</returns>
        public IEnumerable<T> Get<T>(Type targetType, Address address) where T : class
        {
            return Get<T>(targetType, new Type[] { address.Type }, address.GetAsQueryBody<T>());
        }
        /// <summary>
        /// Get items by a query
        /// </summary>
        /// <typeparam name="T">type of items returned by repository</typeparam>
        /// <param name="targetType">type of items these will produce, and in terms of which the query body is defined</param>
        /// <param name="queryBody">function to apply to a source queryable to filter to the queryable required</param>
        /// <returns>items in containers or the items themselves</returns>
        public IEnumerable<T> Get<T>(Type targetType, Func<IQueryable<T>, IQueryable<T>> queryBody) where T : class
        {
            if (typeof(Summary).IsAssignableFrom(targetType))
                return Get<T>(targetType, ContentTypeHierarchy.GetSummaryContainers(targetType), queryBody);
            else
                return Registered(targetType).Get<T>(targetType, new Type[] { targetType }, queryBody);
        }
        /// <summary>
        /// Get items by a query
        /// </summary>
        /// <typeparam name="T">type of items returned by repository</typeparam>
        /// <param name="targetType">type of items these will produce</param>
        /// <param name="types">the possible content types across which the query will be applied</param>
        /// <param name="queryBody">function to apply to a source queryable to filter to the queryable required</param>
        /// <returns>items in containers or the items themselves</returns>
        public IEnumerable<T> Get<T>(Type targetType, IEnumerable<Type> types, Func<IQueryable<T>, IQueryable<T>> queryBody) where T : class
        {
            foreach (var tg in types.GroupBy(t => Registered(t)))
                foreach (var res in tg.Key.Get<T>(targetType, tg, queryBody))
                    yield return (T)res;
        }
        /// <summary>
        /// Get items by path string
        /// </summary>
        /// <typeparam name="T">type of items returned by repository</typeparam>
        /// <param name="targetType">type of items these will produce</param>
        /// <param name="paths">list of paths of items</param>
        /// <returns>items in containers or the items themselves</returns>
        public IEnumerable<ContentItem> GetByPath(Type targetType, List<string> paths)
        {
            return Registered(targetType).Get<ContentItem>(targetType,
                new Type[] { targetType },
                iq => iq.WhereIn(ci => ci.Path, paths));
        }

        /// <summary>
        /// Get total count of items of a given type in api
        /// </summary>
        /// <param name="targetType">type type of item to count</param>
        /// <returns>count</returns>
        public int GetCount(Type targetType)
        {
            Type type = targetType;
            if (Registered(targetType) is ContentRepository)
                type = typeof(ContentItem);

            Type iqType = typeof(IQueryable<>).MakeGenericType(type);

            ParameterExpression iqParam = Expression.Parameter(iqType, "iq");
            LambdaExpression queryExp = Expression.Lambda(iqParam, iqParam);
            return (int)ReflectionX.InvokeGenericMethod(this, "GetCount", type, new Type[] { targetType }, queryExp.Compile());
        }
        /// <summary>
        /// Get count of items matching query
        /// </summary>
        /// <typeparam name="T">type in terms of which query is expressed</typeparam>
        /// <param name="targetType">type of items matching query to count</param>
        /// <param name="queryBody">function to apply to a source queryable to filter to the queryable required</param>
        /// <returns>count of items</returns>
        public int GetCount<T>(IEnumerable<Type> types, Func<IQueryable<T>, IQueryable<T>> queryBody) where T : class
        {
            int count = 0;
            types.GroupBy(t => Registered(t))
                .Do(tg => count += tg.Key.GetCount(tg, queryBody));
            return count;
        }

        /// <summary>
        /// Write a list of objects, determining whether they should be added by whether
        /// their Id has a default value
        /// </summary>
        /// <param name="items">The list of items to add/update</param>
        /// <returns>List of whether the items where added (true) or updated (false)</returns>
        public List<bool> Set(List<object> items)
        {
            return Set(items, (bool?)null);
        }
        /// <summary>
        /// Write a list of objects, optionally specifying they should all be added or
        /// updated
        /// </summary>
        /// <param name="items">The list of items to add/update</param>
        /// <param name="create">If has a value, specifies all items should be added (true) or updated (false), if null, decide from null key on item</param>
        /// <returns>List of whether the items where added (true) or updated (false)</returns>
        public List<bool> Set(List<object> items, bool? create)
        {
            return Set(items, create, false);
        }
                /// <summary>
        /// Write modified items to data store
        /// </summary>
        /// <param name="items">list of modified items</param>
        /// <param name="create">If has a value, specifies all items should be added (true) or updated (false), if null, decide from null key on item</param>
        /// <returns>List of whether the items where added (true) or updated (false)</returns>
        public List<bool> Set(List<object> items, bool? create, bool bypassChecks)
        {
            return Set(items, create, bypassChecks, true);
        }
        /// <summary>
        /// Write or add items to the data store
        /// </summary>
        /// <param name="items">list of items to add/update</param>
        /// <param name="create">If has a value, specifies all items should be added (true) or updated (false), if null, decide from null key on item</param>
        /// <param name="bypassChecks">If true, bypass checks to stop or modify an addition or update used for user-sourced changes</param>
        /// <param name="setAudit">If true set standard audit fields if possible, not if false</param>
        /// <returns>List of whether the items where added (true) or updated (false)</returns>
        public List<bool> Set(List<object> items, bool? create, bool bypassChecks, bool setAudit)
        {
            var setOptions = new Dictionary<string, object>
            {
                { "create", create },
                { "bypassChecks", bypassChecks },
                { "setAudit", setAudit }
            };
            return Set(items, setOptions);
        }
        /// <summary>
        /// Write modified items to data store
        /// </summary>
        /// <param name="items">list of modified items</param>
        /// <param name="create">if true, create record, if null, decide from null key on item</param>
        /// <param name="setAudit">if true, set the audit properties on the item if relevant</param>
        public List<bool> Set(List<object> items, Dictionary<string, object> setOptions)
        {
            if (!setOptions.ContainsKey("setAudit") || (bool)setOptions["setAudit"])
            {
                foreach (var aud in items.OfType<IBasicAuditable>())
                {
                    aud.Updated = DateTime.UtcNow;
                    aud.UserUpdated = SecurityManager.Current?.UserId;
                }
            }
                
            return items.GroupBy(item => Registered(item.GetType()))
                .SelectMany(igroup => igroup.Key.Set(igroup.ToList(), setOptions))
                .ToList();
        }
        /// <summary>
        /// Write a modified item to data store
        /// </summary>
        /// <param name="o">item to write</param>
        public bool Set(object o, bool? create, bool bypassChecks)
        {
            if (o is IEnumerable)
                return Set((o as IEnumerable).Cast<object>().ToList(), create, bypassChecks).Any(created => created);
            else
                return Set(new List<object>{ o }, create, bypassChecks).FirstOrDefault();
        }
        /// <summary>
        /// Add or update a single item
        /// </summary>
        /// <param name="o">The item</param>
        /// <param name="create">If has a value, specifies the item should be added (true) or updated (false), if null, decide from null key on item</param>
        /// <returns>True if added false if updated</returns>
        public bool Set(object o, bool? create)
        {
            return Set(o, create, false);
        }
        /// <summary>
        /// Add or update a single item based on whether key has default value
        /// </summary>
        /// <param name="o">The item</param>
        /// <returns>True if added false if updated</returns>
        public bool Set(object o)
        {
            return Set(o, null);
        }

        /// <summary>
        /// Delete an item from data store
        /// </summary>
        /// <param name="o">item to delete</param>
        public void Delete(object o, bool bypassChecks)
        {
            Registered(o.GetType()).Delete(o, bypassChecks);
        }

        #endregion
    }
}
