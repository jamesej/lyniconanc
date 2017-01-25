using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Models;
using Lynicon.Utility;
using Lynicon.Config;
using Lynicon.Services;

namespace Lynicon.Repositories
{
    /// <summary>
    /// Simple db context to access DbChanges table before data api initialisation
    /// </summary>
    public class PreloadDb : DbContext
    {
        static PreloadDb()
        {
            Database.SetInitializer<PreloadDb>(null);
        }

        public PreloadDb()
            : base(LyniconSystem.Instance.Settings.SqlConnectionString)
        { }

        /// <summary>
        /// The records in the DbChanges table
        /// </summary>
        public DbSet<DbChange> DbChanges { get; set; }
    }
}
