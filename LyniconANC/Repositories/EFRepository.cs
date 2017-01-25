using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Lynicon.Attributes;
using Lynicon.Extensibility;
using Lynicon.Models;
using Lynicon.Utility;
using Lynicon.Linq;
using System.Linq.Expressions;
using System.Collections;
using Lynicon.Collation;
using Lynicon.Membership;
using Microsoft.EntityFrameworkCore;

namespace Lynicon.Repositories
{
    /// <summary>
    /// Repository for the Basic persistence model
    /// </summary>
    [UsesCoreDb]
    public class EFRepository<TContext> : IRepository
        where TContext : DbContext
    {
        /// <summary>
        /// The table name for the table from which the repository gets data
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// The name of the unique Id
        /// </summary>
        public string IdName { get; set; }

        /// <summary>
        /// Specifier for the data source, e.g. a connection string
        /// </summary>
        public string DataSourceSpecifier { get; set; }

        /// <summary>
        /// Set the query timeout
        /// </summary>
        public int? QueryTimeoutSecs { get; set; }

        public Dictionary<Type, Func<TContext, IQueryable>> DbSetSelectors { get; set; }

        /// <summary>
        /// Set how long the context persists for.  Can be per call to the repository or per request
        /// </summary>
        public ContextLifetimeMode ContextLifetimeMode
        {
            get
            {
                return ContextLifetimeMode.PerRequest;
            }
            set
            { }
        }
        /// <summary>
        /// Get the context which the Repository uses for database persistence.  This is a new context
        /// unless ContextLifetimeMode is set to PerRequest in which case it is the instance associated with
        /// the current request
        /// </summary>
        /// <returns>the Repository's database context</returns>
        public virtual TContext GetDb()
        {
            var db = GetDbInner();

            if (QueryTimeoutSecs.HasValue)
                db.Database.SetCommandTimeout(QueryTimeoutSecs.Value);
            else if (Repository.Instance.QueryTimeoutSecs.HasValue)
                db.Database.SetCommandTimeout(Repository.Instance.QueryTimeoutSecs);

            return db;
        }

        protected virtual TContext GetDbInner()
        {
            // Uses the service container to get the context.  Generally this will be scoped by
            // request unless overridden to be Transient in config code
            return (TContext)RequestContextManager.Instance.ScopedServiceProvider.GetService(typeof(TContext));
        }

        /// <summary>
        /// Ends the call, disposing the db context if necessary
        /// </summary>
        /// <param name="db">The db context</param>
        public void EndCallDb(TContext db)
        {
            //if (this.ContextLifetimeMode == ContextLifetimeMode.PerCall || HttpContext.Current.Items == null)
            //    db.Dispose();
        }

        /// <summary>
        /// For database persisted data via entity framework, using the per request context lifetime, allows
        /// the data to be attached to the database context.
        /// </summary>
        /// <param name="item">container item to attach</param>
        /// <param name="isNew">is it a new record</param>
        /// <param name="isModified">has it been modified</param>
        public void AttachToDb(object item, bool isNew, bool isModified)
        {
            var db = GetDb();
            if (db.Entry(item).State != EntityState.Detached)
                return;

            db.Entry(item).State = isNew ? EntityState.Added : (isModified ? EntityState.Modified : EntityState.Unchanged);
            EndCallDb(db);
        }

        /// <summary>
        /// Detach a content item from the db context
        /// </summary>
        /// <param name="item">content item</param>
        public void DetachFromDb(object item)
        {
            var db = GetDb();
            db.Entry(item).State = EntityState.Detached;
            EndCallDb(db);
        }

        /// <summary>
        /// Get the PropertyInfo of the id for a given type
        /// </summary>
        /// <param name="t">The type</param>
        /// <returns>The PropertyInfo for the id</returns>
        public PropertyInfo GetIdProperty(Type t)
        {
            return LinqX.GetIdProp(t, this.IdName);
        }

        /// <summary>
        /// Get the container type (the extended type) from the content type
        /// </summary>
        /// <param name="t">The content type</param>
        /// <returns>The container type</returns>
        public virtual Type ContainerType(Type t)
        {
            Type outputType = t;
            if (CompositeTypeManager.Instance.ExtendedTypes.ContainsKey(outputType))
                outputType = CompositeTypeManager.Instance.ExtendedTypes[outputType];
            return outputType;
        }

        /// <summary>
        /// Create a new EFRepository
        /// </summary>
        public EFRepository()
        {
            TableName = null;
            IdName = null;

            ContextLifetimeMode = ContextLifetimeMode.PerRequest;
            DataSourceSpecifier = null;
            QueryTimeoutSecs = null;

            DbSetSelectors = typeof(TContext).GetProperties()
                .Where(pi => pi.PropertyType.IsGenericType && pi.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .ToDictionary(
                    pi => pi.PropertyType.GenericTypeArguments[0],
                    pi =>
                    {
                        var x = Expression.Parameter(typeof(TContext));
                        var getDbSet = Expression.MakeMemberAccess(x, pi);
                        var castDbSet = Expression.TypeAs(getDbSet, typeof(IQueryable));
                        var selector = Expression.Lambda<Func<TContext, IQueryable>>(castDbSet, x);
                        return selector.Compile();
                    });
        }

        #region IRepository Members

        /// <summary>
        /// Create a new instance of a container type
        /// </summary>
        /// <param name="createType">The container type</param>
        /// <returns>New instance</returns>
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

        /// <summary>
        /// Get a query to find items by unique id
        /// </summary>
        /// <typeparam name="T">Type of the content items to get</typeparam>
        /// <param name="ids">The list of ids of items to get</param>
        /// <returns>A query body</returns>
        public Func<IQueryable, IQueryable> GetIdsQuery<T>(IEnumerable<object> ids) where T : class
        {
            return (Func<IQueryable, IQueryable>)(iq => iq.AsFacade<T>().WhereIn<T>(LinqX.GetIdSelector<T>(), ids));
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
            bool isSummary = typeof(Summary).IsAssignableFrom(targetType);

            TContext db = GetDb();
            try
            {
                foreach (var idg in ids.GroupBy(ii => ii.Type))
                {
                    if (idg.Count() > MaximumIdBatchSize)
                        throw new ArgumentException("Request for too many ids at once, request in batches of maximum size " + MaximumIdBatchSize);

                    var qed = new QueryEventData<IQueryable>
                    {
                        QueryBody = (Func<IQueryable, IQueryable>)ReflectionX.InvokeGenericMethod(this, "GetIdsQuery", idg.Key, idg.Select(ii => ii.Id))
                    };

                    qed.Source = DbSetSelectors[idg.Key]((TContext)db);
                    
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
            finally
            {
                if (!isSummary)
                    EndCallDb((TContext)db);
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

            TContext db = GetDb();

            try
            {
                var qed = new QueryEventData<IQueryable>
                {
                    QueryBody = iq => queryBody(iq.AsFacade<TQuery>()).AsFacade<T>()
                };

                qed.Source = DbSetSelectors[typeof(T)]((TContext)db);

                qed = EventHub.Instance.ProcessEvent("Repository.Get." + (isSummary ? "Summaries" : "Items"), this, qed).Data as QueryEventData<IQueryable>;

                var ftq = qed.QueryBody(qed.Source).AsFacade<T>();

                foreach (var res in ftq)
                    yield return res;
            }
            finally
            {
                EndCallDb((TContext)db);
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

        /// <summary>
        /// Get count of container by query
        /// </summary>
        /// <typeparam name="T">The type of the resulting containers</typeparam>
        /// <param name="types">The allowed types of contained content items returned</param>
        /// <param name="queryBody">An operator on an IQueryable of the container type to filter the ones to count</param>
        /// <returns>The count of containers</returns>
        public virtual int GetCount<T>(IEnumerable<Type> types, Func<IQueryable<T>, IQueryable<T>> queryBody) where T : class
        {
            if (types == null || !types.Any())
                return 0;

            if (types.Count() == 1)
            {
                var db = GetDb();
                try
                {
                    var qed = new QueryEventData<IQueryable>
                    {
                        Source = DbSetSelectors[typeof(T).ContentType()](db),
                        QueryBody = iq => queryBody(iq.AsFacade<T>())
                    };
                    qed = EventHub.Instance.ProcessEvent("Repository.Get.Count", this, qed).Data as QueryEventData<IQueryable>;

                    // TMP AsEnumerable() is a hack to avoid an error
                    return ((IQueryable<T>)qed.QueryBody(qed.Source)).AsEnumerable().Count();
                }
                finally
                {
                    EndCallDb(db);
                }
            }
            else
            {
                int count = 0;
                foreach (Type t in types)
                    count += this.GetCount<T>(new Type[] { t }, queryBody);
                return count;
            }
        }

        /// <summary>
        /// Get fields needed to create a summary type from a record type
        /// </summary>
        /// <typeparam name="T">the record type</typeparam>
        /// <param name="contentType">the content type</param>
        /// <param name="summaryType">the summary type</param>
        /// <returns>list of fields needed to create summary type</returns>
        public virtual List<string> GetFieldsForSummary(Type contentType, Type summaryType)
        {
            //List<string> summaryProps = summaryType.GetProperties().Select(pi => pi.Name).ToList();
            var propertyMap = contentType.GetProperties()
                .Select(pi => new {
                    pi.Name,
                    SummaryAttribute = pi.GetCustomAttribute<SummaryAttribute>(),
                    AddressingAttribute = pi.GetCustomAttribute<AddressComponentAttribute>() })
                .Where(pia => (pia.SummaryAttribute != null) // && summaryProps.Contains(pia.SummaryAttribute.SummaryProperty)
                              || pia.AddressingAttribute != null)
                .Select(pia => pia.Name)
                .ToList();
            propertyMap.Add(LinqX.GetIdProp(contentType, this.IdName).Name);
            return propertyMap;
        }

        /// <summary>
        /// Set (create or update) a list of containers to the data source
        /// </summary>
        /// <param name="items">The list of containers</param>
        /// <param name="setOptions">Options for setting</param>
        /// <returns>List of flags for whether the corresponding by position item was created (rather than updated)</returns>
        public virtual List<bool> Set(List<object> items, Dictionary<string, object> setOptions)
        {
            var db = GetDb();
            var createds = new List<bool>();
            bool? create = setOptions.Get<bool?>("create");
            bool bypassChecks = setOptions.Get<bool>("bypassChecks", false);
            bool anyUnhandled = false;
            try
            {
                var savedItems = new List<Tuple<object, bool>>();
                foreach (object item in items)
                {
                    var idProp = LinqX.GetIdProp(item.GetType(), this.IdName);
                    bool isAdd;

                    if (create == null)
                    {
                        object noId = ReflectionX.GetDefault(idProp.PropertyType);
                        isAdd = idProp.GetValue(item).Equals(noId);
                    }
                    else
                        isAdd = create.Value;
                    var eventData = new RepositoryEventData(item, bypassChecks);
                    var eventResult = EventHub.Instance.ProcessEvent("Repository.Set." + (isAdd ? "Add" : "Update"), this, eventData);
                    var itemSave = ((RepositoryEventData)eventResult.Data).Container;
                    bool wasHandled = ((RepositoryEventData)eventResult.Data).WasHandled;
                    if (!wasHandled)
                        anyUnhandled = true;

                    isAdd = eventResult.EventName.EndsWith("Add");
                    if (isAdd)
                        DoAdd(db, itemSave, idProp, wasHandled);
                    else if (!wasHandled)
                        // We can't be sure that this item hasn't previously been read by other code from the
                        // request context
                        db.SafeUpdate(itemSave);

                    savedItems.Add(Tuple.Create(itemSave, isAdd));
                    createds.Add(isAdd);
                }

                if (savedItems.Count > 0 && anyUnhandled)
                    db.SaveChanges();

                foreach (var savedItem in savedItems)
                {
                    var eventData = new RepositoryEventData(savedItem.Item1, bypassChecks);
                    EventHub.Instance.ProcessEvent("Repository.Saved." + (savedItem.Item2 ? "Add" : "Update"), this, eventData);
                }
                    
                return createds;
            }
            finally
            {
                EndCallDb(db);
            }
        }

        protected virtual void DoAdd(TContext db, object item, PropertyInfo idProp, bool wasHandled)
        {
            if (idProp.PropertyType == typeof(Guid) && (Guid)idProp.GetValue(item) == Guid.Empty)
                idProp.SetValue(item, Guid.NewGuid());
            if (!wasHandled)
            {
                object addItem = CompositeTypeManager.Instance.ConvertToComposite(item);
                db.Add(addItem);
                //db.Entry(addItem).State = EntityState.Added;
            }
            if (item is IBasicAuditable)
            {
                var aud = (IBasicAuditable)item;
                aud.Created = aud.Updated = DateTime.UtcNow;
                aud.UserCreated = aud.UserUpdated = LyniconSecurityManager.Current.UserId;
            }
        }

        /// <summary>
        /// Delete a container from the data source
        /// </summary>
        /// <param name="o">The container to delete</param>
        /// <param name="bypassChecks">Whether to bypass any checks made to stop deletion by a front end user</param>
        public void Delete(object o, bool bypassChecks)
        {
            var db = GetDb();
            try
            {
                var idProp = LinqX.GetIdProp(o.GetType(), this.IdName);
                object noId = ReflectionX.GetDefault(idProp.PropertyType);
                var eventData = new RepositoryEventData(o, bypassChecks);
                var eventResult = EventHub.Instance.ProcessEvent("Repository.Set.Delete", this, eventData);
                var itemDel = ((RepositoryEventData)eventResult.Data).Container;
                bool wasHandled = ((RepositoryEventData)eventResult.Data).WasHandled;
                if (itemDel != null)
                {
                    if (eventResult.EventName.EndsWith("Add"))
                        DoAdd(db, itemDel, idProp, wasHandled);
                    else if (eventResult.EventName.EndsWith("Update") && !wasHandled)
                        db.Entry(itemDel).State = EntityState.Modified;
                    else if (!wasHandled)
                        db.Entry(itemDel).State = EntityState.Deleted;
                    if (!wasHandled)
                        db.SaveChanges();
                }

                var savedEventData = new RepositoryEventData(o, bypassChecks);
                EventHub.Instance.ProcessEvent(eventResult.EventName.Replace("Set", "Saved"), this, savedEventData);
            }
            finally
            {
                EndCallDb(db);
            }
        }

        #endregion
    }
}
