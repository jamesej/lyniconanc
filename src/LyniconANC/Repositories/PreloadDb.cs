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

namespace Lynicon.Repositories
{
    /// <summary>
    /// Simple db context to access DbChanges table before data api initialisation
    /// </summary>
    public class PreloadDb : DbContext
    {
        public PreloadDb(DbContextOptionsBuilder builder)
            : base(builder.Options)
        { }

        /// <summary>
        /// The records in the DbChanges table
        /// </summary>
        public DbSet<DbChange> DbChanges { get; set; }

        /// <summary>
        /// Build a core Lynicon database
        /// </summary>
        public string EnsureCoreDb()
        {
            List<string> actions = new List<string>();

            //bool dbChangesExists = Database
            //         .(@"
            //             SELECT 1 FROM sys.tables AS T
            //             INNER JOIN sys.schemas AS S ON T.schema_id = S.schema_id
            //             WHERE S.Name = 'dbo' AND T.Name = 'DbChanges'")
            //         .SingleOrDefault() != null;


            Database.ExecuteSqlCommand(
                @"IF NOT EXISTS (SELECT 1 FROM sys.tables AS T
                        INNER JOIN sys.schemas AS S ON T.schema_id = S.schema_id
                        WHERE S.Name = 'dbo' AND T.Name = 'DbChanges')
                    CREATE TABLE [dbo].[DbChanges](
	                [Id] [int] IDENTITY(1,1) NOT NULL,
	                [Change] [nvarchar](100) NOT NULL,
	                [ChangedWhen] [datetime] NOT NULL,
                    CONSTRAINT [PK_DbChanges] PRIMARY KEY CLUSTERED (Id))");
            actions.Add("Ensured DbChanges table exists");

            if (this.DbChanges.Any(dbc => dbc.Change.StartsWith("LyniconInit ")))
                return null;

            Database.ExecuteSqlCommand(
                @"CREATE TABLE [dbo].[ContentItems](
	                [Id] [uniqueidentifier] NOT NULL,
	                [Identity] [uniqueidentifier] NOT NULL,
	                [DataType] [varchar](250) NOT NULL,
	                [Path] [nvarchar](250) NULL,
	                [Locale] [varchar](10) NULL,
	                [Summary] [nvarchar](max) NULL,
	                [Content] [nvarchar](max) NULL,
	                [Title] [nvarchar](250) NULL,
	                [Created] [datetime] NOT NULL,
	                [UserCreated] [varchar](40) NULL,
	                [Updated] [datetime] NOT NULL,
	                [UserUpdated] [varchar](40) NULL,
                 CONSTRAINT [PK_ContentItems] PRIMARY KEY CLUSTERED (Id))");
            actions.Add("Created ContentItems table");

            Database.ExecuteSqlCommand(
                @"CREATE TABLE [dbo].[Users](
	                [Id] [uniqueidentifier] NOT NULL,
	                [UserName] [nvarchar](100) NOT NULL,
	                [Email] [nvarchar](128) NULL,
	                [Password] [nvarchar](128) NULL,
	                [Roles] [varchar](30) NULL,
	                [Created] [date] NOT NULL,
	                [Modified] [date] NOT NULL,
                CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED (Id))");
            actions.Add("Created Users table");

            DbChanges.Add(new DbChange { Change = "LyniconInit 0.1", ChangedWhen = DateTime.Now });
            actions.Add("Insert initial DbChange record");

            SaveChanges();
            return actions.Join(", ");
        }
    }
}
