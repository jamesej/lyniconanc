using Lynicon.Extensibility;
using Lynicon.Repositories;
using System;
using System.Collections.Generic;
//using System.Data.Entity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Services;

namespace Lynicon.DataSources
{
    public class CoreDataSource : IDataSource
    {
        public string DataSourceSpecifier { get; set; }

        public int? QueryTimeoutSecs { get; set; }

        protected DbContext Db { get; set; }

        ContextLifetimeMode contextLifetimeMode = ContextLifetimeMode.PerCall;

        public LyniconSystem System { get; set; }

        public CoreDataSource(LyniconSystem sys, ContextLifetimeMode contextLifetimeMode, bool forSummaries)
        {
            this.contextLifetimeMode = contextLifetimeMode;
            System = sys;
            Db = GetDb(forSummaries);
        }

        protected DbContext GetDb(bool forSummaries)
        {
            if (forSummaries)
                return new SummaryDb();

            if (this.contextLifetimeMode == ContextLifetimeMode.PerRequest
                && RequestContextManager.Instance.CurrentContext.Items != null
                && RequestContextManager.Instance.CurrentContext.Items.ContainsKey("_lynicon_request_context"))
                return (CoreDb)RequestContextManager.Instance.CurrentContext.Items["_lynicon_request_context"];

            CoreDb db;
            if (DataSourceSpecifier == null)
                db = new CoreDb();
            else
                db = new CoreDb(DataSourceSpecifier);

            if (this.contextLifetimeMode == ContextLifetimeMode.PerRequest
                && RequestContextManager.Instance.CurrentContext.Items != null)
                RequestContextManager.Instance.CurrentContext.Items.Add("_lynicon_request_context", db);

            // TMP
            //if (QueryTimeoutSecs.HasValue)
            //    db.Database.CommandTimeout = QueryTimeoutSecs.Value;
            //else if (Repository.Instance.QueryTimeoutSecs.HasValue)
            //    db.Database.CommandTimeout = Repository.Instance.QueryTimeoutSecs;

            return db;
        }

        public void Create(object o)
        {
            var item = System.Extender.ConvertToExtended(o);
            Db.Entry(item).State = EntityState.Added;
        }

        public void Delete(object o)
        {
            var item = System.Extender.ConvertToExtended(o);
            Db.Entry(item).State = EntityState.Deleted;
        }

        public IQueryable GetSource(Type type)
        {
            if (Db is SummaryDb)
                return ((SummaryDb)Db).SummarisedSet(type);
            else
                return ((CoreDb)Db).CompositeSet(type);
        }

        public void SaveChanges()
        {
            Db.SaveChanges();
        }

        public void Update(object o)
        {
            var item = System.Extender.ConvertToExtended(o);
            Db.Entry(item).State = EntityState.Modified;
        }

        public void Dispose()
        {
            if (this.contextLifetimeMode == ContextLifetimeMode.PerCall || RequestContextManager.Instance.CurrentContext.Items == null)
                Db.Dispose();
        }
    }
}
