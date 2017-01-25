using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Utility;

namespace Lynicon.Repositories
{
    /// <summary>
    /// Manager for the composite types needed by the system
    /// </summary>
    public class CompositeTypeManager
    {
        static readonly CompositeTypeManager instance = new CompositeTypeManager();
        public static CompositeTypeManager Instance { get { return instance; } }

        /// <summary>
        /// This allows binary deserialization to use types in the dynamically created assembly of types
        /// </summary>
        static CompositeTypeManager()
        {
            AppDomain.CurrentDomain.AssemblyResolve +=
                (object sender, ResolveEventArgs args) =>
                {
                    if (args.Name.UpTo(",") == "CompositeClasses")
                        return CompositeTypeManager.Instance.ExtendedTypes.Values.First().Assembly;
                    else
                        return null;
                };
        }

        /// <summary>
        /// Dictionary of a base type to its extended type
        /// </summary>
        public readonly Dictionary<Type, Type> ExtendedTypes = new Dictionary<Type, Type>();
        /// <summary>
        /// All the base types
        /// </summary>
        public readonly List<Type> BaseTypes = new List<Type>();
        /// <summary>
        /// All the types inheriting from the base type whose properties are used to generate the extended type
        /// </summary>
        public readonly List<Type> ExtensionTypes = new List<Type>();
        /// <summary>
        /// Dictionary of a base type to its summarised type
        /// </summary>
        public readonly Dictionary<Type, Type> SummarisedTypes = new Dictionary<Type, Type>();

        /// <summary>
        /// Register a base type
        /// </summary>
        /// <param name="t">The base type</param>
        public void RegisterType(Type t)
        {
            if (!BaseTypes.Contains(t))
                BaseTypes.Add(t);
        }

        /// <summary>
        /// Register an extension type whose properties are used to extend the base type it inherits from
        /// </summary>
        /// <param name="t">The extension type</param>
        public void RegisterExtensionType(Type t)
        {
            ExtensionTypes.Add(t);
        }

        /// <summary>
        /// Build all the extended (composite) types
        /// </summary>
        public void BuildComposites()
        {
            CompositeClassFactory.Instance.Initialise(BaseTypes);
            foreach (var baseType in BaseTypes)
            {
                Type type = CompositeClassFactory.Instance.GetCompositeClass(baseType,
                    ExtensionTypes.Where(t => t.IsSubclassOf(baseType)).ToList());
                ExtendedTypes.Add(baseType, type);
                Type sumsType = CompositeClassFactory.Instance.GetSummarisedType(type);
                SummarisedTypes.Add(baseType, sumsType);
            }
        }

        /// <summary>
        /// Convert an object into an object of the composite type (if it isn't already)
        /// </summary>
        /// <param name="o">The object</param>
        /// <returns>An object of a composite type</returns>
        public object ConvertToComposite(object o)
        {
            Type t = o.GetType();
            Type baseType = null;
            if (ExtendedTypes.ContainsValue(t))
                return o;
            else if (BaseTypes.Contains(t))
                baseType = t;
            else if (BaseTypes.Contains(t.BaseType))
                baseType = t.BaseType;
            else
                // -- may not be handled by composite system
                //throw new Exception("Type " + t.FullName + " has no composite type");
                return o;

            Type compType = ExtendedTypes[baseType];
            var tProps = t.GetProperties().ToDictionary(pi => pi.Name, pi => pi);
            object comp = Activator.CreateInstance(compType);

            foreach (PropertyInfo pi in compType.GetProperties().Where(pi => tProps.ContainsKey(pi.Name) && pi.CanWrite))
            {
                pi.SetValue(comp, tProps[pi.Name].GetValue(o));
            }

            return comp;
        }
    }
}
