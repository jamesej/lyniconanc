using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Attributes;
using Lynicon.Extensibility;
using Lynicon.Models;
using Lynicon.Relations;
using Lynicon.Repositories;
using Lynicon.Routing;
using Lynicon.Utility;
using Microsoft.AspNetCore.Routing;
using Lynicon.Editors;
using Lynicon.DataSources;
using Lynicon.Services;
using System.ComponentModel.DataAnnotations;

namespace Lynicon.Collation
{
    /// <summary>
    /// The default type of the global collator, which holds registered collators for all the content types
    /// and is the usual API through which client code calls the collator to get data
    /// </summary>
    public class Collator : TypeRegistry<ICollator>, ICollator, ITypeSystemRegistrar
    {
        static Collator instance = null;
        /// <summary>
        /// The global collator
        /// </summary>
        public static Collator Instance
        {
            get
            {
                if (instance == null)
                    instance = new Collator(LyniconSystem.Instance);
                return instance;
            }
            internal set
            {
                instance = value;
            }
        }

        /// <summary>
        /// Get the type of content contained within an object (which might just be the object's type)
        /// </summary>
        /// <param name="o">the container or content item</param>
        /// <returns>the type of the contained object</returns>
        public static Type GetContentType(object o)
        {
            if (o is IContentContainer)
                return ((IContentContainer)o).ContentType;
            else
                return o.GetType().UnextendedType();
        }

        static Collator() { }

        /// <summary>
        /// Whether the global repository associated with the global collator has been built
        /// </summary>
        public bool RepositoryBuilt { get; set; }

        public LyniconSystem System { get; set; }

        /// <summary>
        /// Construct a general collator from its associated global repository
        /// </summary>
        /// <param name="repository"></param>
        public Collator(LyniconSystem sys)
        {
            RepositoryBuilt = false;
            System = sys;
            sys.Collator = this;

            this.DefaultHandler = new ContentCollator(sys) as ICollator;
        }

        public Collator()
        {
            System = LyniconSystem.Instance;
        }

        /// <summary>
        /// The container type this repository uses (or null if its just the content type)
        /// </summary>
        public Type AssociatedContainerType { get { return null; } }

        /// <summary>
        /// Register a collator for a given content type
        /// </summary>
        /// <param name="type">The content type</param>
        /// <param name="typeHandler">The associated collator</param>
        public override void Register(Type type, ICollator typeHandler)
        {
            typeHandler.System = this.System;
            if (typeHandler.AssociatedContainerType != null)
                base.Register(typeHandler.AssociatedContainerType, typeHandler);
            base.Register(type, typeHandler);
        }

        /// <summary>
        /// Get the content contained within an object (which might just be the object itself)
        /// </summary>
        /// <param name="o">the container</param>
        /// <returns>the contained object</returns>
        public object GetContent(object o)
        {
            if (o is IContentContainer)
                return ((IContentContainer)o).GetContent(System.Extender);
            else
                return o;
        }

        /// <summary>
        /// Initialise the core dbcontext, the collator, the repository and the editor redirect for a content type
        /// </summary>
        /// <param name="t">content type</param>
        /// <param name="coll">the collator</param>
        /// <param name="repo">the repository</param>
        /// <param name="redir">the editor redirect</param>
        public void SetupType(Type t, ICollator coll, IRepository repo, Func<IRouter, RouteContext, object, IRouter> divert)
        {
            if ((coll ?? this.Registered(null)).ContainerType(t) == t) // type t is its own container, so it may be extended
                System.Extender.RegisterForExtension(t);

            if (!typeof(IContentContainer).IsAssignableFrom(t))
                ContentTypeHierarchy.RegisterType(t);

            if (coll != null)
            {
                coll.System = this.System;
                this.Register(t, coll);
            }

            if (repo != null)
                System.Repository.Register(t, repo);

            if (divert != null)
                DataDiverter.Instance.Register(t, divert);
        }

        /// <summary>
        /// Build composite types which have been registered
        /// </summary>
        public void BuildRepository()
        {
            ContentTypeHierarchy.AllContentTypes.Do(ct =>
                System.Extender.RegisterForExtension(
                    System.Collator.ContainerType(ct)
                    )
            );
            BuildForTypes(ContentTypeHierarchy.AllContentTypes);
            System.Extender.BuildExtensions(this);
            RepositoryBuilt = true;
            System.Events.ProcessEvent("Repository.Built", this, null);
        }

        public void BuildForTypes(IEnumerable<Type> types)
        {
            types.GroupBy(t => this.Registered(t))
                .Do(tg => tg.Key.BuildForTypes(tg));
        }

        /// <summary>
        /// Get a data item, or list of items, via the route which maps to them
        /// </summary>
        /// <typeparam name="T">type of the item(s), a generic list if a list of items</typeparam>
        /// <param name="rd">route data</param>
        /// <returns>the mapped item(s)</returns>
        public T Get<T>(RouteData rd) where T : class
        {
            return Get<T>(typeof(T), rd);
        }
        /// <summary>
        /// Get a data item, or list of items, via the route which maps to them
        /// </summary>
        /// <typeparam name="T">type of the item(s), a generic list if a list of items, could be a summary type</typeparam>
        /// <param name="contentType">the content type of the item(s)</param>
        /// <param name="rd">route data</param>
        /// <returns>the mapped items(s)</returns>
        public T Get<T>(Type contentType, RouteData rd) where T : class
        {
            //CodeTimer.MarkTime("Get via route START");

            try
            {
                if (typeof(T).IsGenericType() && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type elType = typeof(T).GetGenericArguments()[0];
                    ICollator coll;
                    List<Type> contentTypes;
                    bool isSummary = typeof(Summary).IsAssignableFrom(elType);
                    if (isSummary)
                    {
                        contentTypes = ContentTypeHierarchy.GetSummaryContainers(elType);
                        if (contentTypes.Select(ct => Registered(ct)).Distinct().Count() != 1)
                            throw new Exception("Content types containing summary type " + elType.FullName + " dont have 1 unique registered collator, requirement for a dataroute with list type");

                        coll = Registered(contentTypes.First());
                    }
                    else
                    {
                        coll = Registered(elType);
                        contentTypes = new List<Type> { elType };
                    }

                    T itemList = (T)ReflectionX.InvokeGenericMethod(coll, "GetList",
                        new Type[] { elType, isSummary ? elType : ContainerType(elType) },
                        contentTypes,
                        rd);
                    return itemList;
                }
                else
                {
                    ICollator coll = Registered(contentType);
                    return coll.Get<T>(new List<Address> { coll.GetAddress(contentType, rd) }).FirstOrDefault();
                }
            }
            finally
            {
                //CodeTimer.MarkTime("Get via route END");
            }
            
        }
        /// <summary>
        /// Get a data item via the data address
        /// </summary>
        /// <typeparam name="T">type of the item</typeparam>
        /// <param name="address">data address of the item</param>
        /// <returns>the data item</returns>
        public T Get<T>(Address address) where T : class
        {
            return Get<T>(new List<Address> { address }).FirstOrDefault();
        }

        /// <summary>
        /// Get a data item by its item id
        /// </summary>
        /// <typeparam name="T">return type, this can be a summmary type, a content type, or a class from which several content types inherit</typeparam>
        /// <param name="id">the item id</param>
        /// <returns>content item</returns>
        public T Get<T>(ItemId id) where T : class
        {
            //CodeTimer.MarkTime("Get via single id START");

            var item = Registered(id.Type).Get<T>(new ItemId[] { id }).FirstOrDefault();

            //CodeTimer.MarkTime("Get via single id END");

            return item;
        }
        /// <summary>
        /// Get all items or their summaries assignable to a given type
        /// </summary>
        /// <typeparam name="T">return type, this can be a summmary type, a content type, or a class from which several content types inherit.  The query will be applied across all content types which could output an item of this type.</typeparam>
        /// <returns>list of items of (or cast to) return type</returns>
        public IEnumerable<T> Get<T>()
            where T : class
        {
            return Get<T, object>(iq => iq);
        }
        /// <summary>
        /// Get all content items of a given type or their summaries
        /// </summary>
        /// <typeparam name="T">return type, this can be a summmary type, a content type, or a class from which several content types inherit.  The query will be applied across all content types which could output an item of this type.</typeparam>
        /// <typeparam name="TContent">content type of items to get</typeparam>
        /// <returns>list of items of content type cast to (or summarised as) return type</returns>
        public IEnumerable<T> Get<T, TContent>()
            where T : class
        {
            return Get<T, object>(new Type[] { typeof(TContent) }, iq => iq);
        }
        /// <summary>
        /// Get items via a query
        /// </summary>
        /// <typeparam name="T">return type, this can be a summmary type, a content type, or a class from which several content types inherit.  The query will be applied across all content types which could output an item of this type.</typeparam>
        /// <typeparam name="TQuery">the type in terms of which the query is expressed: the content type or possibly a class from which several content types inherit</typeparam>
        /// <param name="queryBody">a function which takes an iqueryable and adds the query to the end of it</param>
        /// <returns>list of items of (or cast to) return type</returns>
        public IEnumerable<T> Get<T, TQuery>(Func<IQueryable<TQuery>, IQueryable<TQuery>> queryBody)
            where T : class
            where TQuery : class
        {
            if (typeof(Summary).IsAssignableFrom(typeof(T)))
                return Get<T, TQuery>(ContentTypeHierarchy.GetSummaryContainers(typeof(T)), queryBody);
            else
                return Get<T, TQuery>(ContentTypeHierarchy.GetAssignableContentTypes(this, typeof(T)), queryBody);
        }
        /// <summary>
        /// Get data items via a list of data addresses
        /// </summary>
        /// <typeparam name="T">type of the items</typeparam>
        /// <param name="addresses">data addresses of the items</param>
        /// <returns>list of data items</returns>
        public IEnumerable<T> Get<T>(IEnumerable<Address> addresses) where T : class
        {
            //CodeTimer.MarkTime("Get by addresses START");
            try
            {
                foreach (var ag in addresses.Where(a => a != null).GroupBy(a => Registered(a.Type)))
                    foreach (var res in ag.Key.Get<T>(ag))
                        yield return res;
            }
            finally
            {
                //CodeTimer.MarkTime("Get by addresses END");
            }

        }
        /// <summary>
        /// Get data items via a list of ids
        /// </summary>
        /// <typeparam name="T">type to which returned items are cast, can be summary</typeparam>
        /// <param name="ids">ItemIds to find</param>
        /// <returns>enumerable of summary or content types</returns>
        public IEnumerable<T> Get<T>(IEnumerable<ItemId> ids) where T : class
        {
            //CodeTimer.MarkTime("Get by ids START");
            try
            {
            foreach (var idg in ids.Where(id => id != null).GroupBy(id => Registered(id.Type)))
                foreach (var res in idg.Key.Get<T>(idg))
                    yield return res;
            }
            finally
            {
                //CodeTimer.MarkTime("Get by ids END");
            }
        }
        /// <summary>
        /// Get items via a query
        /// </summary>
        /// <typeparam name="T">return type, this can be a summmary type, a content type, or a class from which several content types inherit</typeparam>
        /// <typeparam name="TQuery">the type in terms of which the query is expressed: the content type or possibly a class from which several content types inherit</typeparam>
        /// <param name="types">a list of content types across which the query will be applied</param>
        /// <param name="queryBody">a function which takes an iqueryable and adds the query to the end of it</param>
        /// <returns>list of items of (or cast to) return type</returns>
        public IEnumerable<T> Get<T, TQuery>(IEnumerable<Type> types, Func<IQueryable<TQuery>, IQueryable<TQuery>> queryBody)
            where T : class
            where TQuery : class
        {
            //CodeTimer.MarkTime("Get by query START");
            try
            {
            var tgs = types.Where(t => t != null).GroupBy(t => Registered(t)).ToList();
            foreach (var tg in tgs)
                foreach (var res in tg.Key.Get<T, TQuery>(tg, queryBody))
                    yield return (T)res;
            }
            finally
            {
                //CodeTimer.MarkTime("Get by query END");
            }
        }

        /// <summary>
        /// Get a list of items according to information in routedata
        /// </summary>
        /// <typeparam name="T">type of items in list</typeparam>
        /// <param name="rd">route data of request</param>
        /// <returns>list of items</returns>
        public IEnumerable<T> GetList<T>(RouteData rd) where T : class
        {
            return GetList<T, T>(new Type[] { typeof(T) }, rd);
        }

        /// <summary>
        /// Get a list of items according to information in routedata
        /// </summary>
        /// <typeparam name="T">type of items in list</typeparam>
        /// <param name="rd">route data of request</param>
        /// <returns>list of items</returns>
        public IEnumerable<T> GetList<T, TQuery>(IEnumerable<Type> types, RouteData rd)
            where T : class
            where TQuery : class
        {
            var tgs = types.Where(t => t != null).GroupBy(t => Registered(t)).ToList();
            foreach (var tg in tgs)
                foreach (var res in tg.Key.GetList<T, TQuery>(tg, rd))
                    yield return (T)res;
        }

        /// <summary>
        /// Get new item whose data address is given by a specified route
        /// </summary>
        /// <param name="type">type of new item</param>
        /// <param name="rd">the specified route</param>
        /// <returns>the new item</returns>
        public object GetNew(Type type, RouteData rd)
        {
            return ReflectionX.InvokeGenericMethod(this, "GetNew",
                mi => mi.GetParameters().First().ParameterType == typeof(RouteData),
                new Type[] { type }, rd);
        }
        /// <summary>
        /// Get new item whose address is given
        /// </summary>
        /// <param name="a">the address to create it at</param>
        /// <returns>the new item</returns>
        public object GetNew(Address a)
        {
            return ReflectionX.InvokeGenericMethod(this, "GetNew",
                mi => mi.GetParameters().First().ParameterType == typeof(Address) && mi.GetGenericArguments().Length == 1,
                new Type[] { a.Type }, a);
        }
        /// <summary>
        /// Get new item whose data address is given by a specified route
        /// </summary>
        /// <typeparam name="T">type of new item</typeparam>
        /// <param name="rd">the specified route</param>
        /// <returns>the new item</returns>
        public T GetNew<T>(RouteData rd) where T : class
        {
            return GetNew<T>(rd == null ? (Address)null : new Address(typeof(T), rd));
        }
        /// <summary>
        /// Get new item whose address is given
        /// </summary>
        /// <typeparam name="T">type of the new item</typeparam>
        /// <param name="a">the address to create it at</param>
        /// <returns>the new item</returns>
        public T GetNew<T>(Address a) where T : class
        {
            if (a == null)
            {
                Type extType = System.Extender[typeof(T)] ?? typeof(T);
                var inst = Activator.CreateInstance(extType);
                if (inst is IHasDefaultAddress)
                    a = ((IHasDefaultAddress)inst).GetDefaultAddress();
            }

            return Registered(typeof(T)).GetNew<T>(a);
        }

        public T GetNew<T>(params string[] pathEls) where T : class
        {
            var addr = new Address(typeof(T), string.Join("&", pathEls));
            return GetNew<T>(addr);
        }

        /// <summary>
        /// Save new or modified item to data store (the item must have its data address determined by its contents)
        /// </summary>
        /// <param name="data">the item to save</param>
        /// <returns>true if new record created</returns>
        public bool Set(object data)
        {
            return Set(null, data);
        }
        /// <summary>
        /// Save new or modified item to data store
        /// </summary>
        /// <param name="address">the data address of the item, can be null if not available</param>
        /// <param name="data">the item to save</param>
        /// <returns>true if new record created</returns>
        public bool Set(Address address, object data, bool? create, bool bypassChecks)
        {
            var setOptions = new Dictionary<string, object>();
            if (create.HasValue)
                setOptions.Add("create", create);
            setOptions.Add("bypassChecks", bypassChecks);
            return Set(address, data, setOptions);
        }
        /// <summary>
        /// Save new or modified item to data store
        /// </summary>
        /// <param name="address">the data address of the item, can be null if not available</param>
        /// <param name="data">the item to save</param>
        /// <param name="setOptions">list of options for saving, some may be custom</param>
        /// <returns>true if new record created</returns>
        public bool Set(Address address, object data, Dictionary<string, object> setOptions)
        {
            //CodeTimer.MarkTime("Set START");
            var wasSet = Registered(data.GetType()).Set(address, data, setOptions);
            //CodeTimer.MarkTime("Set END");
            return wasSet;
        }
        /// <summary>
        /// Save new or modified item to data store
        /// </summary>
        /// <param name="data">the item to save</param>
        /// <param name="create">whether to create the item - if null creates if item's id is the default value for its type</param>
        /// <returns>true if new record created</returns>
        public bool Set(object data, bool? create)
        {
            return Set(null, data, create);
        }
        /// <summary>
        /// Save new or modified item to data store
        /// </summary>
        /// <param name="address">the data address of the item, can be null if not available</param>
        /// <param name="data">the item to save</param>
        /// <returns>true if new record created</returns>
        public bool Set(Address address, object data)
        {
            return Set(address, data, (bool?)null);
        }
        /// <summary>
        /// Save new or modified item to data store
        /// </summary>
        /// <param name="address">the data address of the item, can be null if not available</param>
        /// <param name="data">the item to save</param>
        /// <param name="create">whether to create the item - if null creates if item's id is the default value for its type</param>
        /// <returns>true if new record created</returns>
        public bool Set(Address address, object data, bool? create)
        {
            return Set(address, data, create, false);
        }

        /// <summary>
        /// Delete item from data store (the item must have its data address determined by its contents)
        /// </summary>
        /// <param name="data">the item to delete</param>
        public void Delete(object data)
        {
            Delete(null, data);
        }
        /// <summary>
        /// Delete item from data store (address supplied)
        /// </summary>
        /// <param name="address">the address of the item to delete</param>
        /// <param name="data">the item to delete</param>
        public void Delete(Address address, object data)
        {
            Delete(address, data, false);
        }
        /// <summary>
        /// Delete item from data store
        /// </summary>
        /// <param name="address">the data address of the item (can be null if it can be derived from the item to delete)</param>
        /// <param name="data">the item to delete</param>
        public void Delete(Address address, object data, bool bypassChecks)
        {
            Registered(data.GetType()).Delete(address, data, bypassChecks);
        }

        /// <summary>
        /// Move the data address of an item within the data store
        /// </summary>
        /// <param name="contentType">the type of the item</param>
        /// <param name="rd">the route data from which to obtain the new address of the item</param>
        /// <param name="id">the id of the item</param>
        public void MoveAddress(ItemId id, Address moveTo)
        {
            Registered(id.Type).MoveAddress(id, moveTo);
        }

        /// <summary>
        /// Get the data address of an item from type and route
        /// </summary>
        /// <param name="type">the type of the item</param>
        /// <param name="rd">the route data from which to get the data address</param>
        /// <returns>the data address</returns>
        public Address GetAddress(Type type, RouteData rd)
        {
            return Registered(type).GetAddress(type, rd);
        }
        /// <summary>
        /// Get the data address of an item from type and route
        /// </summary>
        /// <typeparam name="T">the type of the item</typeparam>
        /// <param name="rd">the route data from which to get the data address</param>
        /// <returns>the data address</returns>
        public Address GetAddress<T>(RouteData rd) where T : class
        {
            return GetAddress(typeof(T), rd);
        }
        /// <summary>
        /// Get the data address of a container or content item where this is determined by the item itself
        /// </summary>
        /// <param name="o">the container or content item</param>
        /// <returns>the data address</returns>
        public Address GetAddress(object o)
        {
            return Registered(GetContentType(o)).GetAddress(o);
        }

        /// <summary>
        /// Get a specified type summary of a content item or container
        /// </summary>
        /// <typeparam name="T">the type of the summary</typeparam>
        /// <param name="item">item to get summary of</param>
        /// <returns>summary of item</returns>
        public T GetSummary<T>(object item) where T : class
        {
            return Registered(item.GetType()).GetSummary<T>(item);
        }

        /// <summary>
        /// Create a copy of a container with only the properties required to generate
        /// a summary filled in.
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        public object Summarise(object container)
        {
            Type mapType = container.GetType().UnproxiedType();

            object summarised = Activator.CreateInstance(mapType);
            ContainerSummaryFields(mapType).Do(pi => pi.SetValue(summarised, pi.GetValue(container)));

            return summarised;
        }

        /// <summary>
        /// Get a list of (only) the properties of a container type which are required
        /// to generate a summary.
        /// </summary>
        /// <param name="containerType">The container type</param>
        /// <returns>List of PropertyInfos of the properties required to generate a summary</returns>
        public List<PropertyInfo> ContainerSummaryFields(Type containerType)
        {
            Type baseType = TypeExtender.BaseType(containerType);
            var excludedPropNames = new List<string>();
            if (baseType.GetCustomAttribute<SummaryTypeAttribute>() != null) // has a declared summary type
            {
                // Exclude all properties in the base type which aren't marked as in the summary
                excludedPropNames = baseType.GetPersistedProperties()
                    .Where(pi => pi.GetCustomAttribute<SummaryAttribute>() == null 
                                 && pi.GetCustomAttribute<AddressComponentAttribute>() == null
                                 && pi.GetCustomAttribute<KeyAttribute>() == null)
                    .Select(pi => pi.Name)
                    .ToList();
            }

            return containerType.GetPersistedProperties()
                .Where(pi => !excludedPropNames.Contains(pi.Name) && pi.GetCustomAttribute<NotSummarisedAttribute>() == null)
                .ToList();
        }

        /// <summary>
        /// Get a container object containing a content item
        /// </summary>
        /// <param name="item">the content item</param>
        /// <returns>a container containing the content item</returns>
        public object GetContainer(object item)
        {
            return GetContainer(GetAddress(item), item);
        }
        /// <summary>
        /// Get a container object containing a content item
        /// </summary>
        /// <param name="a">the address of the content item</param>
        /// <param name="o">the content item</param>
        /// <returns>a container containing the content item</returns>
        public object GetContainer(Address a, object o)
        {
            if (o is IContentContainer)
                return o as IContentContainer;
            else
            {

                return Registered(o.GetType().UnextendedType()).GetContainer(a, o);
            }
                
        }

        /// <summary>
        /// Get the container type (the extended type) from the content type
        /// </summary>
        /// <param name="contentType">The content type</param>
        /// <returns>The container type</returns>
        public Type ContainerType(Type contentType)
        {
            return Registered(contentType).ContainerType(contentType);
        }

        /// <summary>
        /// Get the Identity property for a container type
        /// </summary>
        /// <param name="t">The container type</param>
        /// <returns>PropertyInfo for the Identity property</returns>
        public PropertyInfo GetIdProperty(Type t)
        {
            return Registered(t).GetIdProperty(t);
        }
    }
}
