using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Attributes;
using Lynicon.Utility;

namespace Lynicon.Linq
{
    /// <summary>
    /// Extension methods for working with IQueryable facades
    /// </summary>
    public static class FacadeTypeX
    {
        /// <summary>
        /// This operator takes a queryable and re-presents it as a queryable of type T.  Subsequent linq operators can then
        /// work with the queryable 'as if' it were type T.  The operator translates these subsequent operations into the
        /// underlying type of the queryable (lets call it U) by converting all member accesses of T into member accesses of U
        /// with the same member name.  When enumerated, a FacadeTypeQueryable will if possible cast the results of type U into
        /// type T directly, or else it will convert U to T by creating a default constructed instance of T and copying as many
        /// properties from U into it as possible.  It is fine to chain multiple AsFacade operations onto the same queryable.
        /// </summary>
        /// <typeparam name="T">The type into which to cast the queryable</typeparam>
        /// <param name="source">The source IQueryable</param>
        /// <returns>Queryable in T (a FacadeTypeQueryable)</returns>
        public static FacadeTypeQueryable<T> AsFacade<T>(this IQueryable source)
        {
            return source.AsFacade<T>(new Dictionary<string, string>());
        }
        /// <summary>
        /// This operator takes a queryable and re-presents it as a queryable of type T.  Subsequent linq operators can then
        /// work with the queryable 'as if' it were type T.  The operator translates these subsequent operations into the
        /// underlying type of the queryable (lets call it U) by converting all member accesses of T into member accesses of U
        /// with the same member name.  When enumerated, a FacadeTypeQueryable will if possible cast the results of type U into
        /// type T directly, or else it will convert U to T by creating a default constructed instance of T and copying as many
        /// properties from U into it as possible.  It is fine to chain multiple AsFacade operations onto the same queryable.
        /// </summary>
        /// <typeparam name="T">The type into which to cast the queryable</typeparam>
        /// <param name="source">The source IQueryable</param>
        /// <param name="propertyMap">Dictionary of mappings between equivalent property names</param>
        /// <returns>Queryable in T (a FacadeTypeQueryable)</returns>
        public static FacadeTypeQueryable<T> AsFacade<T>(this IQueryable source, Dictionary<string, string> propertyMap)
        {
            if (source.GetType().GetGenericTypeDefinition() == typeof(FacadeTypeQueryable<>))
            {
                // visit the current expression to return its type to the underlying type (in case there were earlier calls to AsFacade)
                // then apply this to the original IQueryable and continue with this as the source
                var prov = (source.Provider as FacadeTypeQueryProvider);
                var newExp = prov.DropFacade(source.Expression);
                var newInnerSource = prov.Source.Provider.CreateQuery(newExp);
                return new FacadeTypeQueryable<T>(newInnerSource, propertyMap);
            }

            return new FacadeTypeQueryable<T>(source);
        }

        /// <summary>
        /// Returns the type of an IQueryable with a Facade (which will be a FacadeTypeQueryable) to the underlying type
        /// </summary>
        /// <param name="source">The FacadeTypeQueryable</param>
        /// <returns>An IQueryable in the underlying type</returns>
        public static IQueryable RevertFacade(this IQueryable source)
        {
            if (source.GetType().GetGenericTypeDefinition() == typeof(FacadeTypeQueryable<>))
            {
                var prov = (source.Provider as FacadeTypeQueryProvider);
                var newExp = prov.DropFacade(source.Expression);
                var newInnerSource = prov.Source.Provider.CreateQuery(newExp);
                var sourceType = prov.Source.ElementType;

                // below returns new FacadeTypeQueryable<sourceType>(newInnerSource)
                var facQType = typeof(FacadeTypeQueryable<>).MakeGenericType(sourceType);
                return (IQueryable)facQType.GetConstructor(new Type[] { typeof(IQueryable) }).Invoke(new object[] { newInnerSource });
            }

            throw new Exception("Trying to RevertFacade on a non-Facaded queryable");
        }
    }
}
