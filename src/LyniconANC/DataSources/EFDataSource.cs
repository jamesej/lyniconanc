using Lynicon.Extensibility;
using Lynicon.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Utility;
using Microsoft.EntityFrameworkCore;

namespace Lynicon.DataSources
{
    public class EFDataSource<TContext> : IDataSource
        where TContext : DbContext
    {
        public string DataSourceSpecifier { get; set; }

        public int? QueryTimeoutSecs { get; set; }

        protected TContext Db { get; set; }

        Dictionary<Type, Func<TContext, IQueryable>> dbSetSelectors { get; set; }

        ContextLifetimeMode contextLifetimeMode = ContextLifetimeMode.PerCall;

        public EFDataSource(Dictionary<Type, Func<TContext, IQueryable>> dbSetSelectors, ContextLifetimeMode contextLifetimeMode, bool forSummaries)
        {
            this.contextLifetimeMode = contextLifetimeMode;
            this.dbSetSelectors = dbSetSelectors;
            Db = GetDb();
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

        public void Create(object o)
        {
            Db.Add(o);
        }

        public void Delete(object o)
        {
            Db.Remove(o);
        }

        public IQueryable GetSource(Type type)
        {
            return dbSetSelectors[type](Db);
        }

        public void SaveChanges()
        {
            Db.SaveChanges();
        }

        public void Update(object o)
        {
            Db.SafeUpdate(o);
        }

        public void Dispose()
        {
            if (this.contextLifetimeMode == ContextLifetimeMode.PerCall || RequestContextManager.Instance.CurrentContext.Items == null)
                Db.Dispose();
        }
    }
}
