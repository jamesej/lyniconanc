using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Lynicon.Repositories;
using Lynicon.Utility;
using System.Collections.Concurrent;
using Lynicon.Models;
using Lynicon.Collation;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// TypeExtender manages creation at runtime of subclasses of content and container types extended to include properties
    /// and so implement a registered list of interfaces. It provides conversion between the compile-time base class and the
    /// run-time extended class and facilities to access the extended classes.
    /// </summary>
    public class TypeExtender
    {
        static ConcurrentDictionary<Type, Type> allExtensionBaseTypes = new ConcurrentDictionary<Type, Type>();

        /// <summary>
        /// Get the base type of an extended type: this function acts across all Lynicon systems, it can do this
        /// because an extended type will have only one base type. This differs from simply getting the BaseType via
        /// reflection because it will return the original type if an extended type is not passed as the parameter
        /// </summary>
        /// <param name="extType">Any type: if not an extended type it will be returned<param>
        /// <returns>If an extended type, its base type otherwise the type itself</returns>
        public static Type BaseType(Type extType)
        {
            Type unextType;
            if (allExtensionBaseTypes.TryGetValue(extType, out unextType))
                return unextType;
            else
                return extType;
        }

        Dictionary<Type, Type> typeExtensions = new Dictionary<Type, Type>();
        Dictionary<Type, Type> summarisedTypes = new Dictionary<Type, Type>();
        List<(Type tBase, Type tExt)> extensionRules = new List<(Type tBase, Type tExt)>();
        CompositeClassFactory classFactory = null;
        public List<Type> BaseTypes { get; private set; }
        bool built = false;

        /// <summary>
        /// Create a new type extender with default naming of types and dynamic assembly
        /// </summary>
        public TypeExtender() : this("CompositeClasses", "")
        {
        }
        /// <summary>
        /// Create a new type extender specifying naming
        /// </summary>
        /// <param name="assemblyName">Name of the dynamic assembly to create</param>
        /// <param name="typeNameRoot">string added to dynamic type name in form 'Composite_[typeNameRoot]_[basetypename]'</param>
        public TypeExtender(string assemblyName, string typeNameRoot)
        {
            classFactory = new CompositeClassFactory(assemblyName, typeNameRoot);
            BaseTypes = new List<Type>();
        }

        /// <summary>
        /// Register a type as requiring extensions to be built
        /// </summary>
        /// <param name="t">The type to register</param>
        public void RegisterForExtension(Type t)
        {
            if (BaseTypes.Contains(t))
                return;
            if (built)
                throw new InvalidOperationException("Trying to add an extensible type to TypeExtender after types have been built");

            BaseTypes.Add(t);
        }
        /// <summary>
        /// Register a type as requiring extensions to be built
        /// </summary>
        /// <typeparam name="T">The type to register</typeparam>
        public void RegisterForExtension<T>() => RegisterForExtension(typeof(T));

        /// <summary>
        /// Add a rule which indicates a base type should be extended to include an interface. The base type
        /// can also be an interface, in which case the rule is applied recursively.
        /// </summary>
        /// <param name="baseType">A base type or an interface</param>
        /// <param name="extensionInterfaceType">The interface to use to extend all registered instances of the base type or all types which by extension include the base type</param>
        public void AddExtensionRule(Type baseType, Type extensionInterfaceType)
        {
            if (extensionRules.Any(map => map.tBase.IsAssignableFrom(baseType) && extensionInterfaceType.IsAssignableFrom(map.tExt)))
                return;
            if (built)
                throw new InvalidOperationException("Trying to add an extension rule to TypeExtender after types have been built");

            extensionRules.Add((baseType, extensionInterfaceType));
        }

        /// <summary>
        /// Get the dynamic extended type for a base type
        /// </summary>
        /// <param name="baseType">The base type for which to get the extended type</param>
        /// <returns>The dynamic extended type</returns>
        public Type this[Type baseType]
        {
            get { return typeExtensions.ContainsKey(baseType) ? typeExtensions[baseType] : null; }
        }

        /// <summary>
        /// Gets a dynamic type which 
        /// </summary>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public Type Summarised(Type baseType)
        {
            return summarisedTypes[baseType];
        }

        public Type Base(Type extType)
        {
            return typeExtensions.FirstOrDefault(kvp => kvp.Value == extType).Key ?? extType;
        }

        public IEnumerable<Type> ExtensionTypes()
        {
            return typeExtensions.Values;
        }

        public void BuildExtensions(Collator coll)
        {
            var typeBuilders = classFactory.Initialise(BaseTypes);
            foreach (var baseType in BaseTypes)
            {
                // Recursively reapplies extension rules to the base type until a fixed list of interfaces is achieved
                var interfaces = new Type[] { baseType }
                    .Recurse(t => extensionRules
                        .Where(er => er.tBase.IsAssignableFrom(t))
                        .Select(er => er.tExt))
                    .Where(t => t.IsInterface())
                    .Distinct()
                    .ToList();
                Type type = classFactory.GetCompositeClassByInterfaces(typeBuilders, baseType, interfaces);
                typeExtensions.Add(baseType, type);
                allExtensionBaseTypes.TryAdd(type, baseType);
                Type sumsType = classFactory.GetSummarisedType(coll, typeBuilders, type);
                summarisedTypes.Add(baseType, sumsType);
            }
        }


        /// <summary>
        /// Convert an object into an object of the extended type (if it isn't already)
        /// </summary>
        /// <param name="o">The object</param>
        /// <returns>An object of a composite type</returns>
        public object ConvertToExtended(object o)
        {
            Type t = o.GetType();
            Type baseType = null;
            if (BaseTypes.Contains(t))
                baseType = t;
            else if (BaseTypes.Contains(t.BaseType()))
                baseType = t.BaseType();
            else
                return o;

            Type compType = this[baseType];
            object comp = Activator.CreateInstance(compType);
            CopyProperties(o, t, comp);

            return comp;
        }

        /// <summary>
        /// Convert an extended object to an equivalent object of the base type
        /// </summary>
        /// <param name="o">The extended object</param>
        /// <returns>The unextended object</returns>
        public object ConvertToUnextended(object o)
        {
            Type t = o.GetType();
            var kvpMatch = this.typeExtensions.FirstOrDefault(kvp => kvp.Value == t);
            if (kvpMatch.Key == null)
                return o;
            Type uncompType = kvpMatch.Key;
            object uncomp = Activator.CreateInstance(uncompType);
            CopyProperties(o, uncompType, uncomp);

            return uncomp;
        }

        /// <summary>
        /// Copy the data held in extension properties from one object to another (where the properties exist on the other object)
        /// </summary>
        /// <param name="o0">object to copy extension properties from</param>
        /// <param name="o1">object to copy extension properties to</param>
        public static void CopyExtensionData(object o0, object o1)
        {
            Type t0 = o0.GetType();
            Type t1 = o1.GetType();
            var baseType = BaseType(t0);
            if (typeof(ICoreMetadata).IsAssignableFrom(t0) && o1 is ICoreMetadata)
            {
                ((ICoreMetadata)o0).CopyPropertiesTo((ICoreMetadata)o1);
            }

            if (baseType == t0)
                return;
            foreach (PropertyInfo pi0 in o0.GetType().GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance))
            {
                PropertyInfo pi1 = t1.GetProperty(pi0.Name);
                if (pi1 != null)
                    pi1.SetValue(o1, pi0.GetValue(o0));
            }
        }

        public void CopyProperties(object obj0, Dictionary<string, PropertyInfo> props, object obj1)
        {
            foreach (PropertyInfo pi in obj1.GetType().GetProperties().Where(pi => props.ContainsKey(pi.Name) && pi.CanWrite))
            {
                pi.SetValue(obj1, props[pi.Name].GetValue(obj0));
            }
        }
        public void CopyProperties(object obj0, Type t0, object obj1)
        {
            var tProps = t0.GetProperties().Where(pi => pi.CanRead).ToDictionary(pi => pi.Name, pi => pi);
            CopyProperties(obj0, tProps, obj1);
        }
    }
}
