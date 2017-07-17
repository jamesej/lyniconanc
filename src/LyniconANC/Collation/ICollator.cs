using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Models;
using Lynicon.Relations;
using Lynicon.Repositories;
using Microsoft.AspNetCore.Routing;
using System.Reflection;
using Lynicon.Extensibility;
using Lynicon.Services;

namespace Lynicon.Collation
{
    public interface ICollator
    {
        /// <summary>
        /// The data system in which this collator exists
        /// </summary>
        LyniconSystem System { get; set; }

        /// <summary>
        /// Build extension types and any other actions required to initialise collator based on the types registered for it
        /// </summary>
        /// <param name="types">List of types registered to use this collator</param>
        void BuildForTypes(IEnumerable<Type> types);

        /// <summary>
        /// The container type this repository uses (or null if its just the content type)
        /// </summary>
        Type AssociatedContainerType { get; }

        /// <summary>
        /// Get data items via a list of data addresses
        /// </summary>
        /// <typeparam name="T">type of the items</typeparam>
        /// <param name="addresses">data addresses of the items</param>
        /// <returns>list of data items</returns>
        IEnumerable<T> Get<T>(IEnumerable<Address> addresses) where T : class;

        /// <summary>
        /// Get data items via a list of data addresses
        /// </summary>
        /// <typeparam name="T">type of the items</typeparam>
        /// <param name="addresses">data addresses of the items</param>
        /// <returns>list of data items</returns>
        IEnumerable<T> Get<T>(IEnumerable<ItemId> ids) where T : class;

        /// <summary>
        /// Get items via a query
        /// </summary>
        /// <typeparam name="T">return type, this can be a summmary type, a content type, or a class from which several content types inherit</typeparam>
        /// <typeparam name="TQuery">the type in terms of which the query is expressed: the content type or possibly a class from which several content types inherit</typeparam>
        /// <param name="types">a list of content types across which the query will be applied</param>
        /// <param name="queryBody">a function which takes an iqueryable and adds the query to the end of it</param>
        /// <returns>list of items of (or cast to) return type</returns>
        IEnumerable<T> Get<T, TQuery>(IEnumerable<Type> types, Func<IQueryable<TQuery>, IQueryable<TQuery>> queryBody) where T : class where TQuery : class;

        /// <summary>
        /// Gets a paged list of content items filtered by the OData $filter parameters present in the
        /// request represented by the rd (RouteData) parameter.
        /// </summary>
        /// <typeparam name="T">Returned element type of enumerable</typeparam>
        /// <typeparam name="TQuery">The type in which the $filter parameters are expressed</typeparam>
        /// <param name="types">The list of content types against which the filter is run</param>
        /// <param name="rd">The route data of the current request</param>
        /// <returns>Enumerable of type T of content items filtered and paged</returns>
        IEnumerable<T> GetList<T, TQuery>(IEnumerable<Type> types, RouteData rd) where T : class where TQuery : class;

        /// <summary>
        /// Get new item whose address is given
        /// </summary>
        /// <param name="a">the address to create it at</param>
        /// <returns>the new item</returns>
        T GetNew<T>(Address a) where T : class;

        /// <summary>
        /// Save new or modified item to data store
        /// </summary>
        /// <param name="address">the data address of the item, can be null if not available</param>
        /// <param name="data">the item to save</param>
        /// <param name="setOptions">list of options for saving, some may be custom</param>
        /// <returns>true if new record created</returns>
        bool Set(Address a, object data, Dictionary<string, object> setOptions);

        /// <summary>
        /// Delete item from data store
        /// </summary>
        /// <param name="address">the data address of the item (can be null if it can be derived from the item to delete)</param>
        /// <param name="data">the item to delete</param>
        void Delete(Address a, object data, bool bypassChecks);

        /// <summary>
        /// Move the data address of an item within the data store
        /// </summary>
        /// <param name="contentType">the type of the item</param>
        /// <param name="rd">the route data from which to obtain the new address of the item</param>
        /// <param name="id">the id of the item</param>
        void MoveAddress(ItemId id, Address moveTo);

        /// <summary>
        /// Get the data address of an item from type and route
        /// </summary>
        /// <param name="type">the type of the item</param>
        /// <param name="rd">the route data from which to get the data address</param>
        /// <returns>the data address</returns>
        Address GetAddress(Type type, RouteData rd);
        /// <summary>
        /// Get the data address of a container or content item where this is determined by the item itself
        /// </summary>
        /// <param name="data">the container or content item</param>
        /// <returns>the data address</returns>
        Address GetAddress(object data);

        /// <summary>
        /// Get a specified type summary of a content item or container
        /// </summary>
        /// <typeparam name="T">the type of the summary</typeparam>
        /// <param name="item">item to get summary of</param>
        /// <returns>summary of item</returns>
        T GetSummary<T>(object item) where T : class;

        /// <summary>
        /// Get a container object containing a content item
        /// </summary>
        /// <param name="item">the content item</param>
        /// <returns>a container containing the content item</returns>
        object GetContainer(Address a, object o);

        /// <summary>
        /// Get the container type (the extended type) from the content type
        /// </summary>
        /// <param name="type">The content type</param>
        /// <returns>The container type</returns>
        Type ContainerType(Type type);

        /// <summary>
        /// Get the Identity property for a container type
        /// </summary>
        /// <param name="t">The container type</param>
        /// <returns>PropertyInfo for the Identity property</returns>
        PropertyInfo GetIdProperty(Type t);
    }
}
