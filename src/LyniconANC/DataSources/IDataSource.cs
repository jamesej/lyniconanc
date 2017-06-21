using Lynicon.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.DataSources
{
    public interface IDataSource : IDisposable
    {
        /// <summary>
        /// Return an IQueryable<ExtT> where ExtT is the extended version of 'type' if it exists, or 'type' itself if not,
        /// giving access to the values of type ExtT in the data source
        /// </summary>
        /// <param name="type">The unextended type for which to get the values</param>
        /// <returns>an IQueryable<ExtT> where ExtT is the extended version of 'type' if it exists, or 'type' itself if not</returns>
        IQueryable GetSource(Type type);

        /// <summary>
        /// Update the entity 'o' in the data source (found by matching the key of object o)
        /// </summary>
        /// <param name="o">The updated object</param>
        void Update(object o);

        /// <summary>
        /// Create a new entity 'o' in the data source, and if it has a key property with the DatabaseGeneratedAttribute set
        /// with a value of Identity, this key property is set to a unique incrementing value
        /// </summary>
        /// <param name="o">The new entity</param>
        void Create(object o);

        /// <summary>
        /// Delete the object with the same key as 'o' from the data source
        /// </summary>
        /// <param name="o">The object to delete (match by key)</param>
        void Delete(object o);

        /// <summary>
        /// Apply any update/create/delete changes to the data source in one transaction
        /// </summary>
        void SaveChanges();
    }
}
