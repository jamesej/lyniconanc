using Lynicon.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Utility;
using Microsoft.Extensions.Configuration;
using Lynicon.Collation;
using Microsoft.AspNetCore.Mvc;

namespace Lynicon.Services
{
    /// <summary>
    /// Specification for a module to be created
    /// </summary>
    public class ModuleSpec
    {
        public Type Type { get; set; }
        public object[] Params { get; set; }

        public Type[] GetParamTypes()
        {
            return Params.Select(p => p.GetType()).ToArray();
        }
    }

    /// <summary>
    /// Container for fluently added options for a Lynicon system
    /// </summary>
    public class LyniconSystemOptions
    {
        /// <summary>
        /// The root folder controlled by the File Manager
        /// </summary>
        public string FileManagerRoot { get; set; }

        /// <summary>
        /// Main SQL connection string
        /// </summary>
        public string SqlConnectionString { get; set; }

        /// <summary>
        /// Base view path for finding Lynicon editor views
        /// </summary>
        public string LyniconAreaBaseUrl { get; set; }

        /// <summary>
        /// List of specifications for modules to be created
        /// </summary>
        public List<ModuleSpec> ModuleSpecs { get; private set; }

        /// <summary>
        /// Action to set up and configure non-default handling for types in the Lynicon data system
        /// </summary>
        public Action<ITypeSystemRegistrar> RunTypeSetup { get; private set; }

        public LyniconSystemOptions()
        {
            ModuleSpecs = new List<ModuleSpec>();
        }

        /// <summary>
        /// Bind a configuration section to these options for setup of Lynicon system
        /// </summary>
        /// <param name="configSect">The configuration section to bind</param>
        /// <returns>Modified set of Lynicon options</returns>
        public LyniconSystemOptions UseConfiguration(IConfigurationSection configSect)
        {
            configSect.Bind(this);
            return this;
        }

        /// <summary>
        /// Specify the connection string used for SQL access for this Lynicon data system
        /// </summary>
        /// <param name="connectionString">The connection string to use</param>
        /// <returns>Modified set of Lynicon options</returns>
        public LyniconSystemOptions UseConnectionString(string connectionString)
        {
            SqlConnectionString = connectionString;
            return this;
        }

        /// <summary>
        /// Create and use a module of the indicated type in the Lynicon system
        /// </summary>
        /// <typeparam name="TModule">Type of the module to create</typeparam>
        /// <param name="moduleParams">List of parameters which are passed to an appropriate constructor of the module</param>
        /// <returns>Modified set of Lynicon options</returns>
        public LyniconSystemOptions UseModule<TModule>(params object[] moduleParams) where TModule : Extensibility.Module
        {
            ModuleSpecs.Add(new ModuleSpec { Type = typeof(TModule), Params = moduleParams });

            return this;
        }

        /// <summary>
        /// Apply the supplied action to an ITypeSystemRegistrar in order to correctly set up non-default handling of specified types
        /// in the Lynicon data system
        /// </summary>
        /// <param name="setupTypes">Action to set up handlers of types</param>
        /// <returns>Modified set of Lynicon options</returns>
        public LyniconSystemOptions UseTypeSetup(Action<ITypeSystemRegistrar> setupTypes)
        {
            RunTypeSetup = setupTypes;
            return this;
        }
    }
}
