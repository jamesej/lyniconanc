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
using System.ComponentModel.DataAnnotations;
using Lynicon.Map;
using Lynicon.Linq;
using Lynicon.Relations;
using Microsoft.AspNetCore.Routing;
using Lynicon.Exceptions;
using LyniconANC.Extensibility;
using Lynicon.Services;

namespace Lynicon.Collation
{
    public class BasicCollator : BaseCollator, ICollator
    {
        public BasicCollator(LyniconSystem sys) : base(sys)
        { }

        #region ICollator Members

        public string GetIdName<T>()
        {
            return GetIdName(typeof(T));
        }
        public virtual string GetIdName(Type t)
        {
            return System.Collator.GetIdProperty(t).Name;
        }

        /// <inheritdoc/>
        public override Type AssociatedContainerType { get { return null; } }

        public override void BuildForTypes(IEnumerable<Type> types)
        {
            
        }

        /// <inheritdoc/>
        public override IEnumerable<T> Get<T>(IEnumerable<Address> addresses)
        {
            foreach (var ag in addresses.GroupBy(a => a.Type))
            {
                var typeResults = (IEnumerable<T>)ReflectionX.InvokeGenericMethod(this, "GetAddressesOfType", false, mi => true, new Type[] { typeof(T), ag.Key }, ag);
                foreach (var res in typeResults)
                    yield return res;
            }
        }
        protected IEnumerable<T> GetAddressesOfType<T, TAddress>(IEnumerable<Address> addresses)
            where T : class
            where TAddress : class
        {
            bool isSummary = typeof(T).IsSubclassOf(typeof(Summary)) || typeof(T) == typeof(Summary);
            //foreach (var a in addresses)
            //{
            //    Func<IQueryable<TAddress>, IQueryable<TAddress>> queryBody = a.GetAsQueryBody<TAddress>();
            //    foreach (var res in Repository.Get<TAddress>(typeof(T), queryBody))
            //        yield return isSummary ? GetSummary<T>(res) : res as T;
            //}
            foreach (var res in Collate<object>(null, addresses))
                yield return isSummary ? GetSummary<T>(res) : res as T;
        }

        /// <inheritdoc/>
        public override IEnumerable<T> Get<T>(IEnumerable<ItemId> ids)
        {
            foreach (var idg in ids.GroupBy(id => id.Type))
            {
                var typeResults = (IEnumerable<T>)ReflectionX.InvokeGenericMethod(this, "GetIdsOfType", false, mi => true, new Type[] { typeof(T), idg.Key }, idg);
                foreach (var res in typeResults)
                    yield return res;
            }
        }

        private IEnumerable<T> GetIdsOfType<T, TId>(IEnumerable<ItemId> ids)
            where T : class
            where TId : class
        {
            bool isSummary = typeof(T).IsSubclassOf(typeof(Summary)) || typeof(T) == typeof(Summary);
            foreach (var res in Collate<object>(Repository.Get<TId>(typeof(T), ids), null))
                yield return isSummary ? GetSummary<T>(res) : res as T;
        }

        /// <inheritdoc/>
        public override IEnumerable<T> Get<T, TQuery>(IEnumerable<Type> types, Func<IQueryable<TQuery>, IQueryable<TQuery>> queryBody)
        {
            bool isSummary = typeof(Summary).IsAssignableFrom(typeof(T));
            bool isSummaryQuery = typeof(Summary).IsAssignableFrom(typeof(TQuery));

            if (isSummary)
                foreach (var res in Repository.Get<TQuery>(typeof(T), types, queryBody))
                    yield return GetSummary<T>(res);
            else
            {
                //if (!typeof(T).IsAssignableFrom(typeof(TQuery)))
                //    throw new ArgumentException("Need to be able to assign query type: " + typeof(TQuery).FullName + " to output type " + typeof(T).FullName);

                foreach (var res in Collate<object>(Repository.Get<TQuery>(typeof(T), types, queryBody), null))
                    yield return res as T;
            }
        }

        protected virtual Dictionary<string, string> GetPropertyMap(Type itemType)
        {
            var propertyMap = itemType.GetProperties()
                .Select(pi => new { pi.Name, SummaryAttribute = pi.GetCustomAttribute<SummaryAttribute>() })
                .Where(pia => pia.SummaryAttribute != null)
                .ToDictionary(pia => pia.SummaryAttribute.SummaryProperty ?? pia.Name, pia => pia.Name);
            propertyMap.Add("Id", GetIdName(itemType));
            return propertyMap;
        }

        /// <inheritdoc/>
        public override TTarget GetSummary<TTarget>(object item)
        {
            Type itemType = item.GetType().UnextendedType();
            var propertyMap = GetPropertyMap(itemType);
            var summTypeAttr = itemType.GetCustomAttribute<SummaryTypeAttribute>();
            Type summType = typeof(Summary);
            if (summTypeAttr != null)
                summType = summTypeAttr.SummaryType;

            TTarget summ = Activator.CreateInstance(summType) as TTarget;
            if (summ == null)
                return null;
                //throw new Exception("GetSummary on object with SummaryAttribute indicating class " + summType.FullName + " not assignable to " + typeof(TTarget).FullName);

            propertyMap.Do(kvp => summType.GetProperty(kvp.Key).SetValue(summ,
                                    itemType.GetProperty(kvp.Value).GetValue(item)));
            

            var summary = summ as Summary;
            summary.Type = itemType;
            summary.Url = ContentMap.Instance.GetUrl(item);
            summary.Version = System.Versions.GetVersion(item);
            summary.UniqueId = LinqX.GetIdProp(item.GetType(), null).GetValue(item);

            return summ;
        }

        /// <inheritdoc/>
        public override T GetNew<T>(Address a)
        {
            T newT = System.Repository.New<T>();

            // set values implied by route
            if (a != null && !a.ContainsKey("_id"))  // don't set id from route as it should be automatically generated
                a.SetAddressFields(newT);

            // ensure it is created in the current version
            System.Versions.SetVersion(System.Versions.CurrentVersion, newT);

            return Collate<T>(new object[] { newT }, new Address[] { a }).Single();
        }

        /// <inheritdoc/>
        public override bool Set(Address a, object data, Dictionary<string, object> setOptions)
        {
            if (a == null)
                a = GetAddress(data);
            SetRelated(a.GetAsContentPath(), data, (bool)(setOptions.ContainsKey("bypassChecks") ? setOptions["bypassChecks"] : false));
            return Repository.Set(new List<object>{ data }, setOptions)[0];
        }

        /// <inheritdoc/>
        public override void Delete(Address a, object data, bool bypassChecks)
        {
            Repository.Delete(data, bypassChecks);
        }

        /// <inheritdoc/>
        public override void MoveAddress(ItemId id, Address moveTo)
        {
            var testSumm = Repository.Get(typeof(Summary), new Address[] { moveTo }).FirstOrDefault();
            if (testSumm != null)
                throw new Lynicon.Exceptions.ProhibitedActionException("There is an item already at that address");

            object item = Repository.Get(id.Type, id.Type, id.Id);
            moveTo.SetAddressFields(item);
            System.Events.ProcessEvent("Content.Move", this, Tuple.Create(moveTo, item));
            Repository.Set(item);
        }

        /// <inheritdoc/>
        public override Address GetAddress(Type type, RouteData rd)
        {
            var dict = new Dictionary<string, object>();
            if (rd != null)
            {
                if (rd.Values.ContainsKey("_id"))
                    dict.Add("_id", rd.Values["_id"]);
                else
                {
                    foreach (string key in rd.Values.Keys.Where(k => k.StartsWith("_") || k.StartsWith("*_")))
                        if ((rd.Values[key] ?? "").ToString() != "")
                            dict.Add(key, rd.Values[key]);
                }
            }
            var address = new Address(type, dict);
            return address.FixCase();
        }

        /// <inheritdoc/>
        public override Address GetAddress(object o)
        {
            return new Address(o);
        }

        /// <inheritdoc/>
        public override object GetContainer(Address a, object o)
        {
            return o;
        }

        protected override Type UnextendedContainerType(Type type)
        {
            return type;
        }

        /// <inheritdoc/>
        public override PropertyInfo GetIdProperty(Type t)
        {
            return LinqX.GetIdProp(t, null);
        }

        #endregion
    }
}
