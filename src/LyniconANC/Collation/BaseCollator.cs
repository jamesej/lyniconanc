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

namespace Lynicon.Collation
{
    /// <summary>
    /// A class from which Collators must inherit which captures the common functionalities of
    /// all collators
    /// </summary>
    public abstract class BaseCollator : ICollator
    {
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
    }
}
