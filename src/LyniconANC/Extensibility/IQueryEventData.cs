using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Collation;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// Defines a structure to be used for event data raised by the Repository.Get event
    /// </summary>
    /// <typeparam name="TResult">The type in which the query is expressed</typeparam>
    public interface IQueryEventData<out TResult>
    {
        /// <summary>
        /// An IQueryable on which the query is performed
        /// </summary>
        IQueryable Source { get; set; }
        /// <summary>
        /// A function which maps an IQueryable source to another IQueryable which has been filtered as required
        /// </summary>
        Func<IQueryable, TResult> QueryBody { get; }
        /// <summary>
        /// The Ids requested if this is a request by ids
        /// </summary>
        IEnumerable<ItemId> Ids { get; set; }

        /// <summary>
        /// If this has a value, this is a fast way of returning an alternative list of results which supercedes the
        /// Source and QueryBody: used when a cache is supplying the results
        /// </summary>
        IEnumerable<object> EnumSource { get; set; }
    }
}
