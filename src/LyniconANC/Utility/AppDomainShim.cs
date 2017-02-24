using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Lynicon.Utility
{
#if NETSTANDARD1_6
    public class AppDomain
    {
        public static AppDomain CurrentDomain { get; private set; }

        static AppDomain()
        {
            CurrentDomain = new AppDomain();
        }

        public Assembly[] GetAssemblies()
        {
            var assemblies = new List<Assembly>();
            var dependencies = DependencyContext.Default.RuntimeLibraries;
            foreach (var library in dependencies)
            {
                if (IsCandidateCompilationLibrary(library))
                {
                    var assembly = Assembly.Load(new AssemblyName(library.Name));
                    assemblies.Add(assembly);
                }
            }
            return assemblies.ToArray();
        }

        private static bool IsCandidateCompilationLibrary(RuntimeLibrary compilationLibrary)
        {
            return compilationLibrary.Name == ("LyniconANC")
                || compilationLibrary.Dependencies.Any(d => d.Name.StartsWith("LyniconANC"));
        }
    }
#endif
}
