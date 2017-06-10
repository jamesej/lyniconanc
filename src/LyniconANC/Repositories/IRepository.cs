using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.DataSources;
using Lynicon.Services;

namespace Lynicon.Repositories
{
    /// <summary>
    /// Interface which specifies a provider of Repository functionality
    /// </summary>
    public interface IRepository
    {
        LyniconSystem System { get; set; }
        /// <summary>
        /// The data source factory which the repository uses to get a scoped data source
        /// </summary>
        IDataSourceFactory DataSourceFactory { get; }
        /// <summary>
        /// Create a new instance of a container type
        /// </summary>
        /// <param name="t">The container type</param>
        /// <returns>New instance</returns>
        object New(Type t);
        /// <summary>
        /// Get containers by ids
        /// </summary>
        /// <typeparam name="T">The type of the resulting containers</typeparam>
        /// <param name="targetType">The content type or summary type of the intended output</param>
        /// <param name="ids">The ItemIds of the containers to fetch</param>
        /// <returns></returns>
        IEnumerable<T> Get<T>(Type targetType, IEnumerable<ItemId> ids) where T : class;
        /// <summary>
        /// Get containers by query
        /// </summary>
        /// <typeparam name="T">The type of the resulting containers</typeparam>
        /// <param name="targetType">The content type or summary type of the intended output</param>
        /// <param name="types">The allowed types of contained content items returned</param>
        /// <param name="queryBody">An operator on an IQueryable of the container type to filter the ones to return</param>
        /// <returns>Resulting list of containers</returns>
        IEnumerable<T> Get<T>(Type targetType, IEnumerable<Type> types, Func<IQueryable<T>, IQueryable<T>> queryBody) where T : class;

        /// <summary>
        /// Get count of container by query
        /// </summary>
        /// <typeparam name="T">The type of the resulting containers</typeparam>
        /// <param name="types">The allowed types of contained content items returned</param>
        /// <param name="queryBody">An operator on an IQueryable of the container type to filter the ones to count</param>
        /// <returns>The count of containers</returns>
        int GetCount<T>(IEnumerable<Type> types, Func<IQueryable<T>, IQueryable<T>> queryBody) where T : class;

        /// <summary>
        /// Set (create or update) a list of containers to the data source
        /// </summary>
        /// <param name="items">The list of containers</param>
        /// <param name="setOptions">Options for setting</param>
        /// <returns>List of flags for whether the corresponding by position item was created (rather than updated)</returns>
        List<bool> Set(List<object> items, Dictionary<string, object> setOptions);

        /// <summary>
        /// Delete a container from the data source
        /// </summary>
        /// <param name="o">The container to delete</param>
        /// <param name="bypassChecks">Whether to bypass any checks made to stop deletion by a front end user</param>
        void Delete(object o, bool bypassChecks);
    }
}
