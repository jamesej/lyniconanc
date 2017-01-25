using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Utility;
using Lynicon.Models;
using Lynicon.Linq;
using System.Collections;

namespace Lynicon.Repositories
{
    public class NullRepository : IRepository
    {
        public string DataSourceSpecifier
        {
            get
            {
                return "";
            }

            set
            {
                
            }
        }

        public void Delete(object o, bool bypassChecks)
        {
        }

        /// <summary>
        /// Get a query to find items by unique id
        /// </summary>
        /// <typeparam name="T">Type of the content items to get</typeparam>
        /// <param name="ids">The list of ids of items to get</param>
        /// <returns>A query body</returns>
        public virtual Func<IQueryable, IQueryable> GetIdsQuery<T>(IEnumerable<object> ids) where T : class
        {
            return (Func<IQueryable, IQueryable>)(iq => iq.AsFacade<T>().WhereIn<T>(LinqX.GetIdSelector<T>(), ids));
        }

        public const int MaximumIdBatchSize = 100;

        public IEnumerable<T> Get<T>(Type targetType, IEnumerable<ItemId> ids) where T : class
        {
            bool isSummary = typeof(Summary).IsAssignableFrom(targetType);

            foreach (var idg in ids.GroupBy(ii => ii.Type))
            {
                if (idg.Count() > MaximumIdBatchSize)
                    throw new ArgumentException("Request for too many ids at once, request in batches of maximum size " + MaximumIdBatchSize);

                var qed = new QueryEventData<IQueryable>
                {
                    QueryBody = (Func<IQueryable, IQueryable>)ReflectionX.InvokeGenericMethod(this, "GetIdsQuery", idg.Key, idg.Select(ii => ii.Id))
                };

                qed.Source = Array.CreateInstance(idg.Key, 0).AsQueryable();

                qed.Ids = ids;

                qed = EventHub.Instance.ProcessEvent(isSummary ? "Repository.Get.Summaries.Ids" : "Repository.Get.Items.Ids", this, qed).Data as QueryEventData<IQueryable>;

                if (qed.EnumSource != null)
                    foreach (var res in qed.EnumSource)
                        yield return res as T;
                else
                    foreach (var res in qed.QueryBody(qed.Source).AsFacade<T>())
                        yield return res;
            }
        }

        /// <summary>
        /// Get items by a query body for one specific content type
        /// </summary>
        /// <typeparam name="T">The content type</typeparam>
        /// <typeparam name="TQuery">The type in which the query body is expressed</typeparam>
        /// <param name="targetType">The type to which the content items will be converted</param>
        /// <param name="queryBody">Function adding a filter onto an IQueryable in the content type</param>
        /// <returns>List of content items</returns>
        public virtual IEnumerable<T> BasicGet<T, TQuery>(Type targetType, Func<IQueryable<TQuery>, IQueryable<TQuery>> queryBody)
            where T : class
            where TQuery : class
        {
            bool isSummary = typeof(Summary).IsAssignableFrom(targetType);

            var qed = new QueryEventData<IQueryable>
            {
                QueryBody = iq => queryBody(iq.AsFacade<TQuery>()).AsFacade<T>()
            };

            qed.Source = Array.CreateInstance(typeof(T), 0).AsQueryable();

            qed = EventHub.Instance.ProcessEvent("Repository.Get." + (isSummary ? "Summaries" : "Items"), this, qed).Data as QueryEventData<IQueryable>;

            var ftq = qed.QueryBody(qed.Source).AsFacade<T>();

            foreach (var res in ftq)
                yield return res;
        }

        public IEnumerable<T> Get<T>(Type targetType, IEnumerable<Type> types, Func<IQueryable<T>, IQueryable<T>> queryBody) where T : class
        {
            if (types == null || !types.Any())
                yield break;

            if (types.Count() == 1)
            {
                var contentType = types.Single();

                var itemEnum = (IEnumerable)ReflectionX.InvokeGenericMethod(this, "BasicGet", new Type[] { contentType, typeof(T) }, targetType, queryBody);
                foreach (object res in itemEnum)
                    yield return res as T;
            }
            else
            {
                foreach (Type t in types)
                    foreach (var res in this.Get<T>(targetType, new Type[] { t }, queryBody))
                        yield return res;
            }
        }

        public int GetCount<T>(IEnumerable<Type> types, Func<IQueryable<T>, IQueryable<T>> queryBody) where T : class
        {
            return 0;
        }

        public object New(Type createType)
        {
            if (!CompositeTypeManager.Instance.ExtendedTypes.ContainsValue(createType))
            {
                if (CompositeTypeManager.Instance.ExtendedTypes.ContainsKey(createType))
                    createType = CompositeTypeManager.Instance.ExtendedTypes[createType];
                else
                    throw new Exception("Type " + createType.FullName + " not registered in coredb");
            }
            object newT = Activator.CreateInstance(createType);
            newT = EventHub.Instance.ProcessEvent("Repository.New", this, newT).Data;
            return newT;
        }

        public List<bool> Set(List<object> items, Dictionary<string, object> setOptions)
        {
            return new List<bool>();
        }
    }
}
