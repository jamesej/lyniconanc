using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Attributes;
using Lynicon.Models;
using Lynicon.Utility;
using Lynicon.Config;
using Lynicon.Services;
using Lynicon.Collation;
using Lynicon.DataSources;

namespace Lynicon.Repositories
{
    /// <summary>
    /// The SummaryDb is a different object context from CoreDb so that it is possible to access the same
    /// records using a different type with non-summarised fields.  Trying to do this on the same object context
    /// causes many problems.
    /// </summary>
    public class SummaryDb : DbContext
    {
        private static ConcurrentDictionary<Type, LambdaExpression> projectors = null;
        private static ConcurrentDictionary<Type, MethodInfo> selectors = null;
        private static ConcurrentDictionary<Type, List<string>> alwaysIncludes = null;
        static SummaryDb()
        {
            Database.SetInitializer<SummaryDb>(null);

            projectors = new ConcurrentDictionary<Type, LambdaExpression>();
            selectors = new ConcurrentDictionary<Type, MethodInfo>();
            alwaysIncludes = new ConcurrentDictionary<Type, List<string>>();

            // Prebuilds expressions which are expensive to build because of reflection
            foreach (var baseType in CompositeTypeManager.Instance.SummarisedTypes.Keys)
            {
                projectors.TryAdd(baseType, GetProjector(baseType));
                selectors.TryAdd(baseType, GetSelectMethodInfo(baseType));
                alwaysIncludes.TryAdd(baseType, GetAlwaysIncludes(baseType));
            }
        }

        public SummaryDb()
            : base(LyniconSystem.Instance.Settings.SqlConnectionString)
        {
            this.Configuration.ProxyCreationEnabled = Repository.Instance.NoTypeProxyingInScope;
        }
        public SummaryDb(string nameOrCs)
            : base(nameOrCs)
        {
            this.Configuration.ProxyCreationEnabled = Repository.Instance.NoTypeProxyingInScope;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Ignore(CompositeTypeManager.Instance.SummarisedTypes.Keys);

            if (!Collator.Instance.RepositoryBuilt)
                throw new Exception("In CoreDb.OnModelCreating because there was a use of CoreDb before repository was built");

            var requiredBaseTypes = ContentTypeHierarchy.AllContentTypes
                .Select(ct => Collator.Instance.ContainerType(ct))
                .Select(crt => CompositeTypeManager.Instance.ExtendedTypes.Values.Contains(crt)
                               ? CompositeTypeManager.Instance.ExtendedTypes.First(kvp => kvp.Value == crt).Key
                               : crt)
                .Distinct()
                .Where(crt => Repository.Instance.Registered(crt).DataSourceFactory is CoreDataSourceFactory)
                .ToList();

            var entityMethod = typeof(DbModelBuilder).GetMethod("Entity");

            foreach (var sumsType in CompositeTypeManager.Instance.SummarisedTypes.Where(kvp => requiredBaseTypes.Contains(kvp.Key)))
            {
                string tableName = null;
                var tableNamePi = sumsType.Key.GetProperty("TableName", BindingFlags.Static | BindingFlags.Public);
                if (tableNamePi != null)
                    tableName = (string)tableNamePi.GetValue(null, null);
                if (tableName == null)
                {
                    var tableAttr = sumsType.Key.GetCustomAttribute<TableAttribute>();
                    if (tableAttr != null)
                        tableName = tableAttr.Name;
                    else
                    {
                        tableName = sumsType.Key.Name;
                        if (!tableName.EndsWith("s")) tableName += "s";
                    }
                }

                var summTypeConfig = entityMethod.MakeGenericMethod(sumsType.Value)
                                        .Invoke(modelBuilder, new object[] { });

                var summToTable = summTypeConfig.GetType().GetMethod("ToTable", new Type[] { typeof(string) });
                summToTable.Invoke(summTypeConfig, new object[] { tableName });
            }
        }

        private static LambdaExpression GetProjector(Type tBase)
        {
            Type sumsType = CompositeTypeManager.Instance.SummarisedTypes[tBase];
            Type extType = CompositeTypeManager.Instance.ExtendedTypes[tBase];

            // Codes for x => new ExtType { Prop1 = x.Prop1, Prop2 = x.Prop2 ... }
            var param = Expression.Parameter(sumsType, "x");
            var ctor = Expression.New(extType);
            var bindings = new List<MemberAssignment>();
            foreach (var mapPi in sumsType.GetPersistedProperties())
            {
                var membAcc = Expression.MakeMemberAccess(param, mapPi);
                bindings.Add(Expression.Bind(extType.GetProperty(mapPi.Name), membAcc));
            }
            var memberInit = Expression.MemberInit(ctor, bindings.ToArray());
            var project = Expression.Lambda(memberInit, param);

            return project;
        }

        private static MethodInfo GetSelectMethodInfo(Type tBase)
        {
            Type sumsType = CompositeTypeManager.Instance.SummarisedTypes[tBase];
            Type extType = CompositeTypeManager.Instance.ExtendedTypes[tBase];

            // Get method info for .Select<sumsType, extType>()
            Type iqT = typeof(IQueryable<>).MakeGenericType(sumsType);
            Type funcT = typeof(Func<,>).MakeGenericType(sumsType, extType);
            Type expT = typeof(Expression<>).MakeGenericType(funcT);
            var selectMiG = typeof(Queryable).GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(mi => mi.Name == "Select");
            var selectMi = selectMiG.MakeGenericMethod(sumsType, extType);

            return selectMi;
        }

        private static List<string> GetAlwaysIncludes(Type tBase)
        {
            Type sumsType = CompositeTypeManager.Instance.SummarisedTypes[tBase];
            return sumsType.GetProperties()
                .Where(pi => pi.GetCustomAttribute<AlwaysIncludeAttribute>() != null)
                .Select(pi => pi.Name)
                .ToList();
        }

        /// <summary>
        /// Get an IQueryable in the extended type given the base type, sourcing data without fetching
        /// non-summarised fields
        /// </summary>
        /// <typeparam name="TBase">The base type</typeparam>
        /// <returns>IQueryable in extended type which doesn't fetch non-summarised fields</returns>
        public IQueryable SummarisedSet<TBase>()
        {
            return SummarisedSet(typeof(TBase));
        }
        /// <summary>
        /// Get an IQueryable in the extended type given the base type, sourcing data without fetching
        /// non-summarised fields
        /// </summary>
        /// <param name="tBase">The base type</param>
        /// <returns>IQueryable in extended type which doesn't fetch non-summarised fields</returns>
        public IQueryable SummarisedSet(Type tBase)
        {
            if (!CompositeTypeManager.Instance.BaseTypes.Contains(tBase))
                throw new Exception("No composite of base type " + tBase.FullName);

            // Below hack required because constructing the 'project' query (which is .Select(..) under the covers)
            // causes a database connection to be made, so this must be avoided.  The result of the IQueryable returned
            // is not relevant as it will be ignored, however the item type IS used in TotalCache.
            if (Repository.Instance.AvoidConnection)
            {
                Type itemType = CompositeTypeManager.Instance.ExtendedTypes[tBase];
                Type listType = typeof(List<>).MakeGenericType(itemType);
                return ((IEnumerable)(Activator.CreateInstance(listType))).AsQueryable();
            }
                

            Type sumsType = CompositeTypeManager.Instance.SummarisedTypes[tBase];
            //Type extType = CompositeTypeManager.Instance.ExtendedTypes[tBase];

            DbQuery q = Set(sumsType);
            foreach (string inclName in alwaysIncludes[tBase])
                q = q.Include(inclName);

            var project = projectors[tBase];
            var selectMi = selectors[tBase];

            //var aTX = Expression.New();
            //var qry = ((IQueryable)q.AsNoTracking());

            var qryOut = (IQueryable)selectMi.Invoke(null, new object[] { q, project });


            return qryOut;
        }

    }
}
