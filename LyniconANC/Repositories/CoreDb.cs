using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
//using System.Data.Entity;
//using System.Data.Entity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Dynamic;
using System.Reflection;
using Lynicon.Attributes;
using Lynicon.Config;
using Lynicon.Services;
using Lynicon.Collation;
using Lynicon.Models;
using System.Diagnostics;
using Lynicon.DataSources;
using Lynicon.Utility;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Lynicon.Repositories
{
    /// <summary>
    /// The DbContext used for built in Data API requests, Basic and Content persistence models
    /// </summary>
    public class CoreDb : DbContext
    {
        protected static IModel CoreModel { get; set; }

        protected static void BuildModel()
        {
            //if (!Collator.Instance.RepositoryBuilt)
            //    throw new Exception("In CoreDb.OnModelCreating because there was a use of CoreDb before repository was built");

            Debug.WriteLine("Building CoreDb");

            var requiredTypes = ContentTypeHierarchy.AllContentTypes
                .Select(ct => Collator.Instance.ContainerType(ct))
                .Distinct()
                .Where(crt => Repository.Instance.Registered(crt).DataSourceFactory is CoreDataSourceFactory)
                .ToList();

            var builder = new ModelBuilder(SqlServerConventionSetBuilder.Build());


            foreach (var extendedType in CompositeTypeManager.Instance.ExtendedTypes.Where(kvp => requiredTypes.Contains(kvp.Value)))
            {
                builder.Entity(extendedType.Value).ToTable(LinqX.GetTableName(extendedType.Key));
            }

            CoreModel = builder.Model;
        }

        static CoreDb()
        {
            BuildModel();
        }

        public CoreDb()
            : base(
                  new DbContextOptionsBuilder<CoreDb>()
                  .UseModel(CoreModel)
                  .UseSqlServer(LyniconSystem.Instance.Settings.SqlConnectionString)
                  .Options)
        { }
        public CoreDb(string connectionString)
            : base(
                  new DbContextOptionsBuilder<CoreDb>()
                  .UseModel(CoreModel)
                  .UseSqlServer(connectionString)
                  .Options)
        { }

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
            if (!CompositeTypeManager.Instance.BaseTypes.Contains(tBase))
                throw new Exception("No composite of base type " + tBase.FullName);

            Type extType = CompositeTypeManager.Instance.ExtendedTypes[tBase];

            IQueryable q = (IQueryable)ReflectionX.InvokeGenericMethod(this, "InnerCompositeSet", extType, true, useIncludes);

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
