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

namespace Lynicon.Collation
{
    public class BasicCollator : BaseCollator, ICollator
    {
        public BasicCollator(Repository repository)
        {
            this.Repository = repository;
        }

        #region ICollator Members

        public string GetIdName<T>()
        {
            return GetIdName(typeof(T));
        }
        public string GetIdName(Type t)
        {
            return Collator.Instance.GetIdProperty(t).Name;
        }

        /// <inheritdoc/>
        public override Type AssociatedContainerType { get { return null; } }

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
        private IEnumerable<T> GetAddressesOfType<T, TAddress>(IEnumerable<Address> addresses)
            where T : class
            where TAddress : class
        {
            bool isSummary = typeof(T).IsSubclassOf(typeof(Summary)) || typeof(T) == typeof(Summary);
            foreach (var a in addresses)
            {
                Func<IQueryable<TAddress>, IQueryable<TAddress>> queryBody = a.GetAsQueryBody<TAddress>();
                foreach (var res in Repository.Get<TAddress>(typeof(T), queryBody))
                    yield return isSummary ? GetSummary<T>(res) : res as T;
            }
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
            foreach (var res in Repository.Get<TId>(typeof(T), ids))
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

                foreach (var res in Repository.Get<TQuery>(typeof(T), types, queryBody))
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
            Type itemType = item.GetType().ContentType();
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
            summary.Version = VersionManager.Instance.GetVersion(item);
            summary.UniqueId = LinqX.GetIdProp(item.GetType(), null).GetValue(item);

            return summ;
        }

        /// <inheritdoc/>
        public override T GetNew<T>(Address a)
        {
            T newT = Repository.New<T>();

            // set values implied by route
            if (a != null && !a.ContainsKey("_id"))  // don't set id from route as it should be automatically generated
                a.SetAddressFields(newT);

            // ensure it is created in the current version
            VersionManager.Instance.SetVersion(VersionManager.Instance.CurrentVersion, newT);

            return newT;
        }

        /// <inheritdoc/>
        public override bool Set(Address a, object data, Dictionary<string, object> setOptions)
        {
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
                throw new ApplicationException("There is an item already at that address");

            object item = Repository.Get(id.Type, id.Type, id.Id);
            moveTo.SetAddressFields(item);
            EventHub.Instance.ProcessEvent("Content.Move", this, Tuple.Create(moveTo, item));
            Repository.Set(item);
        }

        /// <inheritdoc/>
        public override Address GetAddress(Type type, RouteData rd)
        {
            Address address = new Address();
            if (rd != null)
            {
                if (rd.Values.ContainsKey("_id"))
                    address.Add("_id", rd.Values["_id"]);
                else
                {
                    foreach (string key in rd.Values.Keys.Where(k => k.StartsWith("_") || k.StartsWith("*_")))
                        if ((rd.Values[key] ?? "").ToString() != "")
                            address.Add(key, rd.Values[key]);
                }
            }
            address.Type = type;
            address.FixCase();
            return address;
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
