using Lynicon.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Linq;
using Microsoft.AspNetCore.Routing;
using System.Reflection;

namespace Lynicon.Collation
{
    /// <summary>
    /// AdaptorCollator wraps an inner collator for handling type TFrom so that it presents an interface
    /// which handles type TTo.  Instances of types and queries using the outer type are converted appropriately
    /// using supplied conversion methods.
    /// </summary>
    /// <typeparam name="TFrom">The type handled by the inner collator</typeparam>
    /// <typeparam name="TTo">The type which the wrapper handles and converts for consumption by the inner collator</typeparam>
    public class AdaptorCollator<TFrom, TTo> : BaseCollator
        where TFrom : class, new()
        where TTo : class, new()
    {
        /// <summary>
        /// Convert from inner to outer type for reading
        /// </summary>
        protected Func<TFrom, TTo> readConvert;

        /// <summary>
        /// Convert from outer to inner type for writing
        /// </summary>
        protected Func<TTo, TFrom, TFrom> writeConvert;

        /// <summary>
        /// Underlying inner collator
        /// </summary>
        protected ICollator baseCollator;

        /// <summary>
        /// Function to convert an id to correct form for inner collator
        /// </summary>
        public Func<object, object> IdWriteConvert { get; set; }
        /// <summary>
        /// Map of outer (key) to inner (value) property names for converting a query in the outer type to the inner type
        /// </summary>
        public Dictionary<string, string> PropertyMap { get; set; }

        /// <summary>
        /// Create a new AdaptorCollator providing collator it adapts, with read and write conversions for the outer
        /// and underlying types
        /// </summary>
        /// <param name="baseCollator">Collator this adaptor wraps</param>
        /// <param name="readConvert">Conversion from data read from inner collator to type of outer adaptor</param>
        /// <param name="writeConvert">Conversion for data being written from outer adaptor to inner collator</param>
        public AdaptorCollator(ICollator baseCollator, Func<TFrom, TTo> readConvert, Func<TTo, TFrom, TFrom> writeConvert)
        {
            this.baseCollator = baseCollator;
            this.readConvert = readConvert;
            this.writeConvert = writeConvert;
            IdWriteConvert = x => x;
            PropertyMap = new Dictionary<string, string>();
            this.Repository = baseCollator.Repository;
        }

        #region ICollator Members

        /// <inheritdoc/>
        public override Type AssociatedContainerType { get { return baseCollator.AssociatedContainerType; } }

        /// <inheritdoc/>
        public override IEnumerable<T> Get<T>(IEnumerable<Address> addresses)
        {
            if (typeof(TTo).IsAssignableFrom(typeof(T)))
                return baseCollator.Get<TFrom>(addresses).Select(x => readConvert(x)).Cast<T>();
            else
                return baseCollator.Get<T>(addresses);
        }
        /// <inheritdoc/>
        public override IEnumerable<T> Get<T>(IEnumerable<ItemId> ids)
        {
            if (typeof(TTo).IsAssignableFrom(typeof(T)))
                return baseCollator.Get<TFrom>(ids).Select(x => readConvert(x)).Cast<T>();
            else
                return baseCollator.Get<T>(ids);
        }
        /// <inheritdoc/>
        public override IEnumerable<T> Get<T, TQuery>(IEnumerable<Type> types, Func<IQueryable<TQuery>, IQueryable<TQuery>> queryBody)
        {
            if (typeof(TTo).IsAssignableFrom(typeof(T)) || typeof(TFrom) == typeof(T))
            {
                if (!typeof(TTo).IsAssignableFrom(typeof(TQuery)) && typeof(TFrom) != typeof(TQuery))
                    throw new ArgumentException("AdaptorCollator can only do queries in types assignable to the output type");

                // Translate the query into the inner type
                Func<IQueryable<TFrom>, IQueryable<TFrom>> transQueryBody = iq => queryBody(iq.AsFacade<TQuery>()).AsFacade<TFrom>(PropertyMap);
                // Run the query and translate the results into the outer type
                return baseCollator.Get<TFrom, TFrom>(new Type[] { typeof(TFrom) }, transQueryBody).Select(x => readConvert(x)).Cast<T>();
            } 
            else
                return baseCollator.Get<T, TQuery>(types, queryBody);
        }
        /// <inheritdoc/>
        protected override int GetCount<TQuery>(IEnumerable<Type> types, Func<IQueryable<TQuery>, IQueryable<TQuery>> queryBodyCount)
        {
            if (!typeof(TTo).IsAssignableFrom(typeof(TQuery)))
                throw new ArgumentException("AdaptorCollator can only do queries in types assignable to the output type");

            Func<IQueryable<TFrom>, IQueryable<TFrom>> transQueryBody = iq => queryBodyCount(iq.AsFacade<TQuery>()).AsFacade<TFrom>(PropertyMap);
            return baseCollator.Repository.GetCount<TFrom>(new Type[] { typeof(TFrom) }, transQueryBody);
        }
        /// <inheritdoc/>
        public override T GetNew<T>(Address a)
        {
            if (typeof(TTo).IsAssignableFrom(typeof(T)))
                return readConvert(baseCollator.GetNew<TFrom>(a)) as T;
            else
                return baseCollator.GetNew<T>(a);
        }

        /// <summary>
        /// Convert an ItemVersionedId for writing to the underlying collator
        /// </summary>
        /// <param name="ivid">The ItemVersionedId</param>
        /// <returns>The converted ItemVersionedId</returns>
        protected virtual ItemVersionedId WriteConvertVersionedId(ItemVersionedId ivid)
        {
            return new ItemVersionedId(typeof(TFrom), IdWriteConvert(ivid.Id), ivid.Version);
        }

        /// <inheritdoc/>
        public override bool Set(Address a, object data, Dictionary<string, object> setOptions)
        {
            if (data is TTo)
            {
                ItemVersionedId ivid = new ItemVersionedId(data);
                var current = baseCollator.Get<TFrom>(new ItemId[] { WriteConvertVersionedId(ivid) }).FirstOrDefault();
                return SetInner(a, current, data as TTo, setOptions);
            }
            else
                return baseCollator.Set(a, data, setOptions);
        }

        /// <summary>
        /// Inner set called for data which needs converting only once the original record has been obtained
        /// </summary>
        /// <param name="a">Address of content item</param>
        /// <param name="current">Current value of content item in underlying type</param>
        /// <param name="data">New value of content item in outer type</param>
        /// <param name="setOptions">options to control set</param>
        /// <returns>True if added (not updated)</returns>
        protected virtual bool SetInner(Address a, TFrom current, TTo data, Dictionary<string, object> setOptions)
        {
            return baseCollator.Set(a, writeConvert(data as TTo, current), setOptions);
        }

        /// <inheritdoc/>
        public override void Delete(Address a, object data, bool bypassChecks)
        {
            if (data is TTo)
            {
                ItemVersionedId ivid = new ItemVersionedId(data);
                var current = baseCollator.Get<TFrom>(new ItemId[] { WriteConvertVersionedId(ivid) }).FirstOrDefault();
                baseCollator.Delete(a, writeConvert(data as TTo, current), bypassChecks);
            }
            else
                baseCollator.Delete(a, data, bypassChecks);
        }

        /// <inheritdoc/>
        public override void MoveAddress(ItemId id, Address moveTo)
        {
            baseCollator.MoveAddress(id, moveTo);
        }

        /// <inheritdoc/>
        public override Address GetAddress(Type type, RouteData rd)
        {
            return baseCollator.GetAddress(type, rd);
        }
        /// <inheritdoc/>
        public override Address GetAddress(object data)
        {
            return baseCollator.GetAddress(data);
        }

        /// <inheritdoc/>
        public override T GetSummary<T>(object item)
        {
            if (item is TTo)
                return baseCollator.GetSummary<T>(writeConvert(item as TTo, new TFrom()));
            else
                return baseCollator.GetSummary<T>(item);
        }

        /// <inheritdoc/>
        public override object GetContainer(Address a, object o)
        {
            object container;
            if (o is TTo)
                container = baseCollator.GetContainer(a, writeConvert(o as TTo, new TFrom()));
            else
                container = baseCollator.GetContainer(a, o);

            if (container is TFrom)
                return readConvert((TFrom)container);
            else
                return container;
        }

        protected override Type UnextendedContainerType(Type type)
        {
            return baseCollator.ContainerType(type);
        }

        public override PropertyInfo GetIdProperty(Type t)
        {
            return baseCollator.GetIdProperty(t);
        }

        #endregion
    }
}
