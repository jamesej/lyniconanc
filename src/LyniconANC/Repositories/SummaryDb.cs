using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
//using System.Data.Entity;
//using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Attributes;
using Lynicon.Models;
using Lynicon.Utility;
using Lynicon.Services;
using Lynicon.Collation;
using Lynicon.DataSources;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

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
            projectors = new ConcurrentDictionary<Type, LambdaExpression>();
            selectors = new ConcurrentDictionary<Type, MethodInfo>();
            alwaysIncludes = new ConcurrentDictionary<Type, List<string>>();

            var extender = LyniconSystem.Instance.Extender;
            // Prebuilds expressions which are expensive to build because of reflection
            foreach (var summType in extender.BaseTypes)
            {
                projectors.TryAdd(summType, GetProjector(summType));
                selectors.TryAdd(summType, GetSelectMethodInfo(summType));
                alwaysIncludes.TryAdd(extender.Summarised(summType), GetAlwaysIncludes(summType));
            }
        }

        public SummaryDb(DbContextOptionsBuilder builder)
            : base(builder.Options)
        { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            Debug.WriteLine("Building SummaryDb");

            var sys = LyniconSystem.Instance;

            var requiredBaseTypes = ContentTypeHierarchy.AllContentTypes
                .Select(ct => Collator.Instance.ContainerType(ct))
                .Select(crt => sys.Extender.ExtensionTypes().Contains(crt)
                               ? sys.Extender.Base(crt)
                               : crt)
                .Distinct()
                .Where(crt => Repository.Instance.Registered(crt).DataSourceFactory is CoreDataSourceFactory)
                .ToList();

            sys.Extender.BaseTypes.Do(t => builder.Ignore(t));

            foreach (var sumsType in sys.Extender.BaseTypes.Where(bt => requiredBaseTypes.Contains(bt)))
            {
                builder.Entity(sys.Extender.Summarised(sumsType)).ToTable(LinqX.GetTableName(sumsType));
            }
        }

        private static LambdaExpression GetProjector(Type tBase)
        {
            var extender = LyniconSystem.Instance.Extender;
            Type sumsType = extender.Summarised(tBase);
            Type extType = extender[tBase];

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
            var extender = LyniconSystem.Instance.Extender;

            Type sumsType = extender.Summarised(tBase);
            Type extType = extender[tBase];

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
            var extender = LyniconSystem.Instance.Extender;

            Type sumsType = extender.Summarised(tBase);
            return sumsType.GetProperties()
                .Where(pi => pi.GetCustomAttribute<AlwaysIncludeAttribute>() != null)
                .Select(pi => pi.Name)
                .ToList();
        }

        /// <summary>
        /// Get an IQueryable in the extended type given the base type, sourcing data without fetching
        /// non-summarised fields
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <returns>IQueryable in extended type which doesn't fetch non-summarised fields</returns>
        public IQueryable SummarisedSet<T>() where T : class
        {
            IQueryable<T> q = Set<T>().AsNoTracking();
            foreach (string inclName in alwaysIncludes[typeof(T)])
                q = q.Include(inclName);

            return q;
        }
        /// <summary>
        /// Get an IQueryable in the extended type given the base type, sourcing data without fetching
        /// non-summarised fields
        /// </summary>
        /// <param name="tBase">The base type</param>
        /// <returns>IQueryable in extended type which doesn't fetch non-summarised fields</returns>
        public IQueryable SummarisedSet(Type tBase)
        {
            var extender = LyniconSystem.Instance.Extender;
            if (!extender.BaseTypes.Contains(tBase))
                throw new Exception("No composite of base type " + tBase.FullName);

            // Below hack required because constructing the 'project' query (which is .Select(..) under the covers)
            // causes a database connection to be made, so this must be avoided.  The result of the IQueryable returned
            // is not relevant as it will be ignored, however the item type IS used in TotalCache.
            if (Repository.Instance.AvoidConnection)
            {
                Type itemType = extender[tBase] ?? tBase;
                Type listType = typeof(List<>).MakeGenericType(itemType);
                return ((IEnumerable)(Activator.CreateInstance(listType))).AsQueryable();
            }
                
            Type sumsType = extender.Summarised(tBase);
            IQueryable q = (IQueryable)ReflectionX.InvokeGenericMethod(this, "SummarisedSet", sumsType);

            var project = projectors[tBase];
            var selectMi = selectors[tBase];

            var qryOut = (IQueryable)selectMi.Invoke(null, new object[] { q, project });

            return qryOut;
        }

    }
}
