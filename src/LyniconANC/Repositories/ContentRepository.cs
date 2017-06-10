using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Extensibility;
using Lynicon.Models;
using Lynicon.Utility;
using Lynicon.Linq;
using System.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using Lynicon.Collation;
using Lynicon.Membership;
using Lynicon.Attributes;
using Lynicon.DataSources;
using Lynicon.Exceptions;
using Lynicon.Services;

namespace Lynicon.Repositories
{
    /// <summary>
    /// Repository for the Content persistence model
    /// </summary>
    public class ContentRepository : IRepository
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ContentRepository));

        /// <summary>
        /// Store for current change problems used to block writing content types where it might cause data loss
        /// </summary>
        public static List<ChangeProblem> ChangeProblems { get; set; }

        static ContentRepository()
        {
            ChangeProblems = new List<ChangeProblem>();
        }

        /// <summary>
        /// Whether this ContentRepository blocks on a change problem
        /// </summary>
        public bool BypassChangeProblems { get; set; }

        public IDataSourceFactory DataSourceFactory { get; set; }

        public LyniconSystem System { get; set; }

        public ContentRepository(IDataSourceFactory dataSourceFactory)
            : this(LyniconSystem.Instance, dataSourceFactory)
        { }
        public ContentRepository(LyniconSystem sys, IDataSourceFactory dataSourceFactory)
        {
            BypassChangeProblems = false;
            DataSourceFactory = dataSourceFactory;
            System = sys;
            System.Repository.Register(typeof(ContentItem), this);
        }

        #region IRepository Members

        /// <summary>
        /// Create a new instance of a container type
        /// </summary>
        /// <param name="t">The container type</param>
        /// <returns>New instance</returns>
        public virtual object New(Type t)
        {
            if (!typeof(ContentItem).IsAssignableFrom(t))
            {
                if (ContentTypeHierarchy.AllContentTypes.Contains(t))
                    throw new ArgumentException("Type " + t.FullName + " is registered as using ContentRepository, but you are trying to create a new container for it of the same type as the content itself.  You may want to register this type as using BasicRepository in LyniconConfig.cs");
                else
                    throw new ArgumentException("Content repository can only return ContentItem assignable type");
            }
                
            object newT = Activator.CreateInstance(System.Extender[typeof(ContentItem)]);
            ContentItem ci = (ContentItem)newT;
            ci.Identity = Guid.NewGuid();
            newT = EventHub.Instance.ProcessEvent("Repository.New", this, newT).Data;
            return newT;
        }

        public const int MaximumIdBatchSize = 100;

        /// <summary>
        /// Get containers by ids
        /// </summary>
        /// <typeparam name="T">The type of the resulting containers</typeparam>
        /// <param name="targetType">The content type or summary type of the intended output</param>
        /// <param name="ids">The ItemIds of the containers to fetch</param>
        /// <returns></returns>
        public virtual IEnumerable<T> Get<T>(Type targetType, IEnumerable<ItemId> ids) where T : class
        {
            //if (!typeof(T).IsAssignableFrom(ContainerType()))
            //    throw new ArgumentException("Content repository can only return ContentItem type");

            if (!ids.Any()) return new List<T>();

            bool isSummary = typeof(Summary).IsAssignableFrom(targetType);

            var iis = ids.Select(ii => ii.Id);

            if (iis.Count() > MaximumIdBatchSize)
                throw new ArgumentException("Request for too many ids at once, request in batches of maximum size " + MaximumIdBatchSize);

            var qed = new QueryEventData<IQueryable>
            {
                QueryBody = iq => iq.AsFacade<ContentItem>()
                    .WhereIn<ContentItem>((Expression<Func<ContentItem, Guid>>)(ci => ci.Identity), iis),
                Ids = ids
            };

            using (var dataSource = DataSourceFactory.Create(isSummary))
            {
                qed.Source = dataSource.GetSource(typeof(ContentItem));

                //if (!Cache.IsTotalCached(typeof(T), isSummary))
                //    qed.Source = qed.Source.AsNoTracking();

                string eventName = string.Format("Repository.Get.{0}.Ids", isSummary ? "Summaries" : "Items");
                qed = EventHub.Instance.ProcessEvent(eventName, this, qed).Data as QueryEventData<IQueryable>;

                if (qed.EnumSource != null)
                    return qed.EnumSource.Cast<T>().ToList();

                var query = qed.QueryBody(qed.Source).AsFacade<T>(); //.Take(iis.Count());
                var res = query.ToList();
                return res;
            }
        }

        /// <summary>
        /// Get containers by query
        /// </summary>
        /// <typeparam name="T">The type of the resulting containers</typeparam>
        /// <param name="targetType">The content type or summary type of the intended output</param>
        /// <param name="types">The allowed types of contained content items returned</param>
        /// <param name="queryBody">An operator on an IQueryable of the container type to filter the ones to return</param>
        /// <returns>Resulting list of containers</returns>
        public virtual IEnumerable<T> Get<T>(Type targetType, IEnumerable<Type> types, Func<IQueryable<T>, IQueryable<T>> queryBody) where T : class
        {
            //if (!typeof(T).IsAssignableFrom(ContainerType()))
            //    throw new ArgumentException("Content repository can only return ContentItem type");

            bool isSummary = typeof(Summary).IsAssignableFrom(targetType);

            var qed = new QueryEventData<IQueryable>
            {
                //QueryBody = iq => queryBody(
                //        iq.AsFacade<ContentItem>()
                //            .WhereIn(ci => ci.DataType, types.Select(t => t.FullName))
                //            .AsFacade<T>()
                //    )
                QueryBody = iq => queryBody(iq.AsFacade<T>()).AsFacade<ContentItem>()
                            .WhereIn(ci => ci.DataType, types.Select(t => t.FullName))
                            .AsFacade<T>()
            };
            
            // if types contain object, don't select by type
            if (types.Contains(typeof(object)))
                qed.QueryBody = iq => queryBody(iq.AsFacade<T>());

            using (var dataSource = DataSourceFactory.Create(isSummary))
            {
                qed.Source = dataSource.GetSource(typeof(ContentItem));

                //if (!Cache.IsTotalCached(typeof(T), isSummary))
                //    qed.Source = qed.Source.AsNoTracking();

                string eventName = string.Format("Repository.Get.{0}", isSummary ? "Summaries" : "Items");
                qed = EventHub.Instance.ProcessEvent(eventName, this, qed).Data as QueryEventData<IQueryable>;

                return qed.QueryBody(qed.Source).AsFacade<T>().ToList();
            }
        }

        /// <summary>
        /// Get count of container by query
        /// </summary>
        /// <typeparam name="T">The type of the resulting containers</typeparam>
        /// <param name="types">The allowed types of contained content items returned</param>
        /// <param name="queryBody">An operator on an IQueryable of the container type to filter the ones to count</param>
        /// <returns>The count of containers</returns>
        public virtual int GetCount<T>(IEnumerable<Type> types, Func<IQueryable<T>, IQueryable<T>> queryBody) where T : class
        {
            //if (!typeof(T).IsAssignableFrom(ContainerType()))
            //    throw new ArgumentException("Content repository can only return ContentItem type");

            using (var dataSource = DataSourceFactory.Create(true))
            {
                var qed = new QueryEventData<IQueryable>
                {
                    Source = dataSource.GetSource(typeof(ContentItem)),
                    QueryBody = iq => queryBody(
                        iq.AsFacade<ContentItem>()
                          .WhereIn(ci => ci.DataType, types.Select(t => t.FullName))
                          .AsFacade<T>()
                        )
                };

                qed = EventHub.Instance.ProcessEvent("Repository.Get.Count", this, qed).Data as QueryEventData<IQueryable>;

                return qed.QueryBody(qed.Source).AsFacade<ContentItem>().Count();
            }
        }

        /// <summary>
        /// Set (create or update) a list of containers to the data source
        /// </summary>
        /// <param name="items">The list of containers</param>
        /// <param name="setOptions">Options for setting</param>
        /// <returns>List of flags for whether the corresponding by position item was created (rather than updated)</returns>
        public virtual List<bool> Set(List<object> items, Dictionary<string, object> setOptions)
        {
            if (!(items.All(i => i is ContentItem)))
                throw new ArgumentException("Content repository can only set ContentItem type");

            var createds = new List<bool>();
            bool? create = setOptions.Get<bool?>("create");
            bool bypassChecks = setOptions.Get<bool>("bypassChecks", false);

            using (var dataSource = DataSourceFactory.Create(false))
            {
                var ciSaved = new List<Tuple<ContentItem, bool>>();

                bool anyUnhandled = false;
                foreach (ContentItem ci in items)
                {
                    if (ChangeProblems.Any(cp => cp.TypeName == ci.DataType) && !BypassChangeProblems)
                        throw new ProhibitedActionException("Changes in the structure of the data may cause data loss, please advise an administrator");
                    bool isAdd = (create == null ? ci.Id == Guid.Empty : create.Value);
                    var eventData = new RepositoryEventData(ci, bypassChecks);
                    var eventResult = EventHub.Instance.ProcessEvent("Repository.Set." + (isAdd ? "Add" : "Update"), this, eventData);
                    var ciSave = (ContentItem)((RepositoryEventData)eventResult.Data).Container;
                    bool wasHandled = ((RepositoryEventData)eventResult.Data).WasHandled;
                    if (!wasHandled)
                        anyUnhandled = true;
                    isAdd = eventResult.EventName.EndsWith("Add");
                    createds.Add(isAdd);
                    if (isAdd)
                        DoAdd(dataSource, ciSave, wasHandled);
                    else if (!wasHandled)
                        dataSource.Update(ciSave);

                    ciSaved.Add(Tuple.Create(ciSave, isAdd));
                }

                if (ciSaved.Count > 0 && anyUnhandled)
                {
                    dataSource.SaveChanges();
                }
                    
                foreach (var sv in ciSaved)
                {
                    var savedEventData = new RepositoryEventData(sv.Item1, bypassChecks);
                    EventHub.Instance.ProcessEvent("Repository.Saved." + (sv.Item2 ? "Add" : "Update"), this, savedEventData);
                }
                    
            }

            return createds;
        }

        private void DoAdd(IDataSource dataSource, ContentItem item, bool wasHandled)
        {
            if (item.Id == Guid.Empty)
                item.Id = Guid.NewGuid();

            if (!wasHandled)
                dataSource.Create(item);

            item.Created = item.Updated = DateTime.UtcNow;
            item.UserCreated = item.UserUpdated = SecurityManager.Current?.UserId;
        }

        /// <summary>
        /// Delete a container from the data source
        /// </summary>
        /// <param name="o">The container to delete</param>
        /// <param name="bypassChecks">Whether to bypass any checks made to stop deletion by a front end user</param>
        public virtual void Delete(object o, bool bypassChecks)
        {
            if (!(o is ContentItem))
                throw new ArgumentException("Content repository can only delete ContentItem type");

            ContentItem ci = o as ContentItem;
            using (var dataSource = DataSourceFactory.Create(false))
            {
                var eventData = new RepositoryEventData(ci, bypassChecks);
                var eventResult = EventHub.Instance.ProcessEvent("Repository.Set.Delete", this, eventData);
                var ciSave = (ContentItem)((RepositoryEventData)eventResult.Data).Container;
                bool wasHandled = ((RepositoryEventData)eventResult.Data).WasHandled;
                if (ciSave != null)
                {
                    if (eventResult.EventName.EndsWith("Add"))
                        DoAdd(dataSource, ciSave, wasHandled);
                    else if (eventResult.EventName.EndsWith("Update") && !wasHandled)
                        dataSource.Update(ciSave);
                    else if (!wasHandled)
                        dataSource.Delete(ciSave);

                    if (!wasHandled)
                        dataSource.SaveChanges();

                    var savedEventData = new RepositoryEventData(ciSave, bypassChecks);
                    EventHub.Instance.ProcessEvent(eventResult.EventName.Replace("Set", "Saved"), this, savedEventData);
                }
            }
        }

        #endregion
    }
}
