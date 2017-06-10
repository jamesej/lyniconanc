using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Config;
using Lynicon.Repositories;
using Lynicon.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Routing;
using System.Diagnostics;
using System.Reflection;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// The global manager for the lifecycle of modules
    /// </summary>
    public class LyniconModuleManager
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(LyniconModuleManager));

        static LyniconModuleManager instance = new LyniconModuleManager();
        public static LyniconModuleManager Instance { get { return instance; } set { instance = value; } }

        static LyniconModuleManager() { }

        /// <summary>
        /// List of all registered modules
        /// </summary>
        public Dictionary<string, Module> Modules { get; private set; }
        /// <summary>
        /// List of all unblocked modules in initialisation sequence
        /// </summary>
        public ConstraintOrderedCollection<Module> ModuleSequence { get; private set; }
        /// <summary>
        /// Whether the ModuleManager has initialised all its modules
        /// </summary>
        public bool Initialised { get; set; }
        /// <summary>
        /// Whether to skip checking the database for whether it has been configured
        /// appropriately for each module starting up
        /// </summary>
        public bool SkipDbStateCheck { get; set; }

        public LyniconModuleManager()
        {
            Modules = new Dictionary<string, Module>();
            Initialised = false;
            SkipDbStateCheck = false;
        }

        /// <summary>
        /// Register a module to be activated.  Only normally to be called in LyniconConfig.
        /// </summary>
        /// <param name="module">The module to activate</param>
        public void RegisterModule(Module module)
        {
            if (Modules.Values
                .Any(m => m.GetType().IsAssignableFrom(module.GetType())
                       || module.GetType().IsAssignableFrom(m.GetType())))
                throw new ArgumentException("Module exists of related type to module being registered of type " + module.GetType().FullName);
            Modules.Add(module.Name, module);
        }

        public void ExcludeTypeFromModules(string moduleNamespace, Type type)
        {
            foreach (var module in Modules.Values)
            {
                if (module.Name.StartsWith(moduleNamespace))
                    module.NeverAppliesTo.Add(type);
            }
        }

        /// <summary>
        /// Get the registered module of the supplied type
        /// </summary>
        /// <typeparam name="T">Module type</typeparam>
        /// <returns>The registered module of that type</returns>
        public T GetModule<T>() where T : Module
        {
            return Modules.Values.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Whether any modules were blocked on initialisation
        /// </summary>
        public bool AnyBlocked
        {
            get
            {
                return Modules.Values.Any(m => m.Blocked);
            }
        }


        /// <summary>
        /// Call this to register the routes in all modules into the supplied route builder.
        /// </summary>
        /// <param name="builder">Route builder</param>
        public void MapRoutes(IRouteBuilder builder)
        {
            Modules.Values.Do(m => m.MapRoutes(builder));
        }

        /// <summary>
        /// Find and block any module whose prerequisites for operation are not present.  This minimises the damage caused by
        /// inconsistent module configuration.  Sets up the ModuleSequence property with the operating modules.
        /// </summary>
        public void ValidateModules()
        {
            foreach (Module module in Modules.Values)
            {
                if (module.DependentOn.Any(mn => !Modules.Keys.Any(k => k.StartsWith(mn) && k != module.Name)))
                {
                    module.Blocked = true;
                    log.ErrorFormat("Module {0} blocked, missing module it depends on", module.Name);
                    module.Error = "Missing module depended on: " + module.Name;
                }
                else if (module.IncompatibleWith.Any(mn => Modules.Keys.Any(k => k.StartsWith(mn) && k != module.Name)))
                {
                    module.Blocked = true;
                    log.ErrorFormat("Module {0} blocked, incompatible module exists", module.Name);
                    module.Error = "Incompatible module exists: " + module.Name;
                }
            }
            var moduleDict = Modules.Values.Where(m => !m.Blocked).ToDictionary(m => m.Name, m => m);
            var sortedModules = new ConstraintOrderedCollection<Module>(m => m.Name);
            moduleDict.Values
                .Do(m => sortedModules.Add(m, new OrderConstraint(m.Name, m.MustFollow, m.MustPrecede)));
            this.Modules = moduleDict;
            this.ModuleSequence = sortedModules;
        }

        /// <summary>
        /// Run the initialise method on all operating modules in the correct order.  If initialisation
        /// fails or returns false, the module is blocked, which may in a cascade block other modules
        /// dependent on it.
        /// </summary>
        public void Initialise()
        {
            foreach (Module module in ModuleSequence)
            {
                if (module.DependentOn.Any(mn => Modules[mn].Blocked))
                {
                    module.Blocked = true;
                    log.ErrorFormat("Module {0} dependent on a blocked module", module.Name);
                    module.Error = "Dependent on blocked module";
                    continue;
                }

                try
                {
                    bool initialised = module.Initialise();
                    if (!initialised)
                    {
                        module.Blocked = true;
                        log.ErrorFormat("Module {0} blocked, initialiser returned false", module.Name);
                        if (module.Error == null)
                            module.Error = "Initialiser returned false";
                    }
                }
                catch (Exception ex)
                {
                    module.Blocked = true;
                    log.Error("Module " + module.Name + " blocked, initialisation exception", ex);
                    module.Error = "Initialisation exception: " + ex.ToHtml();
                }
            }

            Initialised = true;

            // Tell UI to show an alert if there was a problem and this is the primary module manager
            if (this.AnyBlocked && this == Instance)
                LyniconUi.Instance.ShowProblemAlert = true;

            // Run any registered startup processes

            Task.Run(() => EventHub.Instance.ProcessEvent("StartupProcess", null, null));
        }

        /// <summary>
        /// Calls the Shutdown method on all operating modules in sequence
        /// </summary>
        public void Shutdown()
        {
            Debug.WriteLine("Shutdown code ***");
            log.Info("Shutdown begins");
            foreach (Module module in ModuleSequence)
            {
                try
                {
                    module.Shutdown();
                }
                catch (Exception ex)
                {
                    log.Error("Module " + module.Name + " failed shutdown: ", ex);
                }
            }
            log.Info("Shutdown completed");
        }
    }
}
