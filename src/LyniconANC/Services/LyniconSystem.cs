using Lynicon.Extensibility;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Utility;
using Lynicon.Collation;
using Lynicon.Membership;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Lynicon.Repositories;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Lynicon.Models;
using Microsoft.EntityFrameworkCore;

namespace Lynicon.Services
{
    /// <summary>
    /// Container for a Lynicon data system which specifies how data for a range of type-classes is persisted and versioned
    /// together with modules providing additional services and event handling
    /// </summary>
    public class LyniconSystem
    {
        /// <summary>
        /// The primary Lynicon system which is attached to Lynicon's management UI. Other Lynicon systems must be managed
        /// through their code APIs.
        /// </summary>
        public static LyniconSystem Instance { get; set; }

        /// <summary>
        /// The modules active in this system
        /// </summary>
        public LyniconModuleManager Modules { get; set; }

        /// <summary>
        /// The user/security manager for this system
        /// </summary>
        public ISecurityManager SecurityManager { get; set; }

        /// <summary>
        /// The collator for this system responsible for extraction of content from content containers and combination
        /// of data from different sources
        /// </summary>
        public Collator Collator { get; set; }

        public Repository Repository { get; set; }

        public EventHub Events { get; set; }

        public TypeExtender Extender { get; set; }

        public LyniconSystemOptions Settings { get; set; }

        public VersionManager Versions { get; set; }

        public LyniconSystem()
        {
            Modules = new LyniconModuleManager();
            Collator = new Collator(this);
            Repository = new Repository(this);
            Events = new EventHub();
            Extender = new TypeExtender();
            Versions = new VersionManager();
        }
        public LyniconSystem(LyniconSystemOptions options) : this()
        {
            this.Settings = options;
        }

        public void SetAsPrimarySystem()
        {
            Instance = this;
            LyniconModuleManager.Instance = this.Modules;
            Collator.Instance = this.Collator;
            Repository.Instance = this.Repository;
            EventHub.Instance = this.Events;
            VersionManager.Instance = this.Versions;
        }

        public void Construct(IApplicationBuilder app)
        {
            foreach (var moduleSpec in this.Settings.ModuleSpecs)
            {
                var module = (Extensibility.Module)app.ApplicationServices.CreateInstanceWithParameters(moduleSpec.Type, moduleSpec.Params);

                Modules.RegisterModule(module);
            }
        }
        /// <summary>
        /// Construct outside an ASP.Net Core application
        /// </summary>
        /// <param name="modules"></param>
        public void Construct(IEnumerable<Extensibility.Module> modules)
        {
            foreach (var module in modules)
                Modules.RegisterModule(module);
        }

        /// <summary>
        /// Initialise outside an ASP.Net Core application
        /// </summary>
        public void Initialise()
        {
            Initialise(null, null);
        }
        /// <summary>
        /// Standard Startup file initialisation
        /// </summary>
        /// <param name="app">Application Builder</param>
        /// <param name="life">Application Lifetime</param>
        public void Initialise(IApplicationBuilder app, IApplicationLifetime life)
        {
            if (app != null)
            {
                RequestContextManager.Instance = new RequestContextManager(app.ApplicationServices);

                // Initialise user system
                this.SecurityManager = Membership.SecurityManager.Current = app.ApplicationServices.GetService<ISecurityManager>();
                this.SecurityManager.InitialiseDataApi();

                var partManager = app.ApplicationServices.GetService<ApplicationPartManager>();
                var controllerFeature = new ControllerFeature();
                partManager.PopulateFeature(controllerFeature);
                ContentTypeHierarchy.RegisterControllers(controllerFeature.Controllers);
            }

            Modules.ValidateModules();

            Settings.RunTypeSetup?.Invoke(Collator);
            if (Settings.ApplyDbContext == null) // use SqlServer setup by default
                Settings = Settings.UseDefaultDbContextBuilder((builder, conn) => builder.UseSqlServer(conn));

            Collator.BuildRepository();
            Modules.Initialise(this);

            if (life != null)
                life.ApplicationStopped.Register(Modules.Shutdown);
        }

        /// <summary>
        /// Get the base app folder relative path for the Lynicon views
        /// </summary>
        /// <param name="name">The name of the view file</param>
        /// <returns>Folder relative path</returns>
        public string GetViewPath(string name)
        {
            return "~" + Settings.LyniconAreaBaseUrl + "Views/Shared/" + name;
        }

        /// <summary>
        /// Not complete
        /// </summary>
        /// <param name="container"></param>
        /// <param name="superimposition"></param>
        /// <returns></returns>
        public string CopyVersion(object container, ItemVersion superimposition)
        {
            var summ = this.Collator.GetSummary<Summary>(container);
            var newVersion = summ.Version.Superimpose(superimposition);
            if (newVersion == summ.Version)
                return null;

            var rulesMask = this.Versions.RulesMask(newVersion);

            if (!rulesMask.Any(kvp => kvp.Value == null)) // 
                return null;

            // Get summaries of all versions of item
            Guid identity = (Guid)summ.Id;
            using (var ctx = Versions.PushState(VersioningMode.Specific, rulesMask))
            {
                var versionSumms = this.Collator.Get<Summary, ICoreMetadata>(iq => iq.Where(cm => cm.Identity == identity));
                //var actions =
            }

            return null;
        }
    }
}
