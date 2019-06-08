using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
//using System.Data.Entity;
//using System.Data.Entity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Dynamic;
using System.Reflection;
using Lynicon.Attributes;
using Lynicon.Services;
using Lynicon.Collation;
using Lynicon.Models;
using System.Diagnostics;
using Lynicon.DataSources;
using Lynicon.Utility;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Logging;

namespace Lynicon.Repositories
{
    /// <summary>
    /// The DbContext used for built in Data API requests, Basic and Content persistence models
    /// </summary>
    public class CoreDb : DbContext
    {
        private static readonly LoggerFactory debugLoggerFactory =
            new LoggerFactory(new[] {
            new Microsoft.Extensions.Logging.Debug.DebugLoggerProvider()
            });

        readonly Func<DbContextOptionsBuilder, DbContextOptionsBuilder> optionsApplier;

        public CoreDb(Func<DbContextOptionsBuilder, DbContextOptionsBuilder> optionsApplier)
            : base()
        {
            this.optionsApplier = optionsApplier;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder = this.optionsApplier(optionsBuilder).UseLoggerFactory(debugLoggerFactory);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            var requiredTypes = ContentTypeHierarchy.AllContentTypes
                .Select(ct => Collator.Instance.ContainerType(ct))
                .Distinct()
                .Where(crt => Repository.Instance.Registered(crt).DataSourceFactory is CoreDataSourceFactory)
                .ToList();

            var sys = LyniconSystem.Instance;

            foreach (Type baseType in sys.Extender.BaseTypes.Where(t => requiredTypes.Contains(sys.Extender[t])))
            {
                builder.Entity(sys.Extender[baseType]).ToTable(LinqX.GetTableName(baseType));
            }

            base.OnModelCreating(builder);
        }

        public IQueryable InnerCompositeSet<T>(bool useIncludes) where T : class
        {
            IQueryable<T> q = Set<T>();
            if (useIncludes)
            {
                foreach (var inclPi in typeof(T).GetProperties().Where(pi => pi.GetCustomAttribute<AlwaysIncludeAttribute>() != null))
                    q = q.Include(inclPi.Name);
            }

            return (IQueryable)q;
        }
        /// <summary>
        /// Get a DbQuery (in the extended type) for a given base type
        /// </summary>
        /// <param name="tBase">The base type</param>
        /// <param name="useIncludes">Whether to use includes specified by AlwaysIncludesAttribute</param>
        /// <returns>A DbQuery against the underlying database</returns>
        public IQueryable CompositeSet(Type tBase, bool useIncludes)
        {
            var extender = LyniconSystem.Instance.Extender;
            var ext = extender[tBase];
            if (ext == null)
                throw new Exception("No composite of base type " + tBase.FullName);

            IQueryable q = (IQueryable)ReflectionX.InvokeGenericMethod(this, "InnerCompositeSet", ext, true, useIncludes);

            return q;
        }
        /// <summary>
        /// Get a DbQuery (in the extended type) for a given base type
        /// </summary>
        /// <param name="tBase">The base type</param>
        /// <returns>A DbQuery against the underlying database</returns>
        public IQueryable CompositeSet(Type tBase)
        {
            return CompositeSet(tBase, useIncludes: true);
        }
    }
}
