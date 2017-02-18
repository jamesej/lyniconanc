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
    public class ModuleSpec
    {
        public Type Type { get; set; }
        public object[] Params { get; set; }

        public Type[] GetParamTypes()
        {
            return Params.Select(p => p.GetType()).ToArray();
        }
    }

    public class LyniconSystemOptions
    {
        public string FileManagerRoot { get; set; }

        public string SqlConnectionString { get; set; }

        public string LyniconAreaBaseUrl { get; set; }

        public List<ModuleSpec> ModuleSpecs { get; private set; }

        public Action<ITypeSystemRegistrar> RunTypeSetup { get; private set; }

        public LyniconSystemOptions()
        {
            ModuleSpecs = new List<ModuleSpec>();
        }

        public LyniconSystemOptions UseConfiguration(IConfigurationSection configSect)
        {
            configSect.Bind(this);
            return this;
        }

        public LyniconSystemOptions UseConnectionString(string connectionString)
        {
            SqlConnectionString = connectionString;
            return this;
        }

        public LyniconSystemOptions UseModule<TModule>(params object[] moduleParams) where TModule : Extensibility.Module
        {
            ModuleSpecs.Add(new ModuleSpec { Type = typeof(TModule), Params = moduleParams });

            return this;
        }

        public LyniconSystemOptions UseTypeSetup(Action<ITypeSystemRegistrar> setupTypes)
        {
            RunTypeSetup = setupTypes;
            return this;
        }
    }
}
