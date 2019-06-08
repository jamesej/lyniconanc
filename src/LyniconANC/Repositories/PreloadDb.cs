using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
//using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Models;
using Lynicon.Utility;
using Lynicon.Services;
using Microsoft.EntityFrameworkCore;
using Lynicon.Membership;

namespace Lynicon.Repositories
{
    /// <summary>
    /// Simple db context to access DbChanges table before data api initialisation
    /// </summary>
    public class PreloadDb : DbContext
    {
        readonly Func<DbContextOptionsBuilder, DbContextOptionsBuilder> optionsApplier;

        public PreloadDb(Func<DbContextOptionsBuilder, DbContextOptionsBuilder> optionsApplier)
            : base()
        {
            this.optionsApplier = optionsApplier;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder = this.optionsApplier(optionsBuilder);
        }

        /// <summary>
        /// The records in the DbChanges table
        /// </summary>
        public DbSet<DbChange> DbChanges { get; set; }

        public DbSet<ContentItem> ContentItems { get; set; }

        public DbSet<User> Users { get; set; }

        /// <summary>
        /// Build a core Lynicon database
        /// </summary>
        public string EnsureCoreDb()
        {
            this.Database.EnsureCreated();

            DbChanges.Add(new DbChange { Change = "LyniconInit 0.1", ChangedWhen = DateTime.Now });
            SaveChanges();

            return "Created";
        }
    }
}
