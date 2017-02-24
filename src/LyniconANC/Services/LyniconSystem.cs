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

namespace Lynicon.Services
{
    public class LyniconSystem
    {
        public static LyniconSystem Instance { get; set; }

        static LyniconSystem()
        {
            Instance = new LyniconSystem();
        }

        public LyniconModuleManager Modules { get; set; }

        public ISecurityManager SecurityManager { get; set; }

        public Collator Collator { get; set; }

        public LyniconSystemOptions Settings { get; set; }

        public LyniconSystem()
        {
            Modules = LyniconModuleManager.Instance;
            Collator = Collator.Instance;
        }
        public LyniconSystem(LyniconSystemOptions options) : this()
        {
            this.Settings = options;
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
            }

            Modules.ValidateModules();

            Settings.RunTypeSetup?.Invoke(Collator);

            Collator.BuildRepository();
            Modules.Initialise();

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
    }
}
