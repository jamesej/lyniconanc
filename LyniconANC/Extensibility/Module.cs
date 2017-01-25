using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Repositories;
using Lynicon.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Routing;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// Base class with shared functionality for all modules
    /// </summary>
    public abstract class Module
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Name of the module, must be unique
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Names of modules which must be registered for this one to function
        /// </summary>
        public List<string> DependentOn { get; set; }
        /// <summary>
        /// Names of modules which must be registered before this one (if they are registered at all)
        /// </summary>
        public List<string> MustFollow { get; set; }
        /// <summary>
        /// Names of modules which must be registered after this one (if they are registered at all)
        /// </summary>
        public List<string> MustPrecede { get; set; }
        /// <summary>
        /// Names of modules which cannot operate at the same time as this one
        /// </summary>
        public List<string> IncompatibleWith { get; set; }
        /// <summary>
        /// If true, the module was blocked from starting up and is not operational
        /// </summary>
        public bool Blocked { get; set; }
        /// <summary>
        /// View name of management panel view for this module
        /// </summary>
        public string ManagerView { get; set; }
        /// <summary>
        /// Error which stopped the module initialising
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Create a module supplying its name and the names of any modules on which it is dependent.
        /// </summary>
        /// <param name="name">Name of the module</param>
        /// <param name="dependentOn">Names (if any) of modules on which it is dependent</param>
        public Module(string name, params string[] dependentOn)
        {
            Name = name;
            DependentOn = dependentOn == null ? new List<string>() : dependentOn.ToList();
            MustFollow = DependentOn;
            MustPrecede = new List<string>();
            IncompatibleWith = new List<string>();
            Blocked = false;
            ManagerView = null;
            Error = null;
        }

        /// <summary>
        /// Called to allow the module to register any routes it needs
        /// </summary>
        public virtual void MapRoutes(IRouteBuilder builder)
        { }

        /// <summary>
        /// Called to initialise the module
        /// </summary>
        /// <returns>True if the module initialised successfully, false if not (in which case the module will be blocked)</returns>
        public abstract bool Initialise();

        /// <summary>
        /// Get a ModuleAdminViewModel to pass into the view for rendering the module's status and operations in the Admin page
        /// </summary>
        /// <returns>A ModuleAdminViewModel describing the module</returns>
        public virtual ModuleAdminViewModel GetViewModel()
        {
            return new ModuleAdminViewModel { Title = this.Name };
        }

        /// <summary>
        /// Called to allow the module to perform any shutdown actions when the site is shutting down
        /// </summary>
        public virtual void Shutdown()
        { }

        /// <summary>
        /// Called in order to check that a given database schema change record is present, to ensure
        /// the database schema fits the requirements of the module
        /// </summary>
        /// <param name="changePresent">The string describing the change to check for</param>
        /// <returns></returns>
        protected bool VerifyDbState(string changePresent)
        {
            bool verified = false;
            if (LyniconModuleManager.Instance.SkipDbStateCheck)
                return true;
            try
            {
                var db = new PreloadDb();
                verified = db.DbChanges.Any(dbc => dbc.Change == changePresent);
                if (!verified)
                    log.Warn("Failed to verify database in correct state for: " + changePresent);
            }
            catch (Exception ex)
            {
                log.Fatal("Failed to connect to database", ex);
                throw new Exception("Failed to connect to database", ex);
            }
            return verified;

        }
    }
}
