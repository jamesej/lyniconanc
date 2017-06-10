using Lynicon.Extensibility;
using Lynicon.Repositories;
using Lynicon.Services;
using Lynicon.Utility;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.DataSources
{
    public class EFDataSourceFactory<TContext> : IDataSourceFactory
        where TContext : DbContext
    {
        public string DataSourceSpecifier { get; set; }

        public int? QueryTimeoutSecs { get; set; }

        public Dictionary<Type, Func<TContext, IQueryable>> DbSetSelectors { get; set; }

        public LyniconSystem System { get; set; }

        public EFDataSourceFactory(LyniconSystem sys)
        {
            System = sys;
            DbSetSelectors = typeof(TContext).GetProperties()
                .Where(pi => pi.PropertyType.IsGenericType() && pi.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
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

        public IDataSource Create(bool forSummaries)
        {
            return new EFDataSource<TContext>(System, DbSetSelectors, ContextLifetimeMode, forSummaries);
        }

        ContextLifetimeMode contextLifetimeMode = ContextLifetimeMode.PerCall;
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
    }
}
