using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Dynamic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lynicon.Attributes;
//using Lynicon.Collation;
using Lynicon.Utility;
using Lynicon.Collation;
using LyniconANC.Extensibility;

namespace Lynicon.Repositories
{
    /// <summary>
    /// Copied and extended from Linq.Dynamic.ClassFactory
    /// </summary>
    internal class CompositeClassFactory
    {
        public static readonly CompositeClassFactory Instance = new CompositeClassFactory();

        public const string CompositeClassAssembly = "CompositeClasses";

        static CompositeClassFactory() { }  // Trigger lazy initialization of static fields

        ModuleBuilder module;
        //Dictionary<Signature, Type> classes;
        int classCount;
        ReaderWriterLockSlim rwLock;

        Dictionary<Type, TypeBuilder> typeBuilders = new Dictionary<Type,TypeBuilder>();

        private CompositeClassFactory()
        {
            AssemblyName name = new AssemblyName(CompositeClassAssembly);
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
#if ENABLE_LINQ_PARTIAL_TRUST
            new ReflectionPermission(PermissionState.Unrestricted).Assert();
#endif
            try
            {
                module = assembly.DefineDynamicModule("Module");
            }
            finally
            {
#if ENABLE_LINQ_PARTIAL_TRUST
                PermissionSet.RevertAssert();
#endif
            }
            //classes = new Dictionary<Signature, Type>();
            rwLock = new ReaderWriterLockSlim();
        }

        /// <summary>
        /// Initialise the composite class factory
        /// </summary>
        /// <param name="baseTypes">the base types to build composites for</param>
        public Dictionary<Type, TypeBuilder> Initialise(List<Type> baseTypes)
        {
            rwLock.EnterWriteLock();

            try
            {
                foreach (var baseType in baseTypes)
                {
                    if (baseType.IsSealed() || baseType.GetCustomAttribute<NonCompositeAttribute>() != null)
                        continue;
                    string typeName = "Composite_" + classCount.ToString() + baseType.Name;
                    TypeBuilder tb = this.module.DefineType(typeName, TypeAttributes.Class |
                            TypeAttributes.Public, baseType);
                    typeBuilders.Add(baseType, tb);
                    classCount++;

                    return typeBuilders;
                }
            }
            finally
            {
                rwLock.ExitWriteLock();
            }

            return null;
        }

        /// <summary>
        /// Dynamically generate a composite class which inherits from a base type and includes all the properties and
        /// applicable interfaces from a list of types which inherit from the base type
        /// </summary>
        /// <param name="baseType">The base type</param>
        /// <param name="derivedTypes">List of types inheriting from the base type</param>
        /// <returns>Dynamically created composite of the given types</returns>
        public Type GetCompositeClass(Type baseType, List<Type> derivedTypes)
        {
            if (baseType.IsSealed() || baseType.GetCustomAttribute<NonCompositeAttribute>() != null || derivedTypes.Count == 0)
                return baseType;

            var dpComparer = new StringSelectorComparer<DynamicProperty>(dp => dp.Name, false);
            var typeComparer = new StringSelectorComparer<Type>(t => t.FullName, false);
            //List<Type> derivedTypes = ReflectionX.GetAllDerivedTypes(baseType).ToList();

            var dynamicProperties =
                derivedTypes
                .SelectMany(t => t
                    .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null && p.GetCustomAttribute<NonCompositeAttribute>() == null)
                    .Select(p => new DynamicProperty(p.Name, p.PropertyType)))
                .Distinct(dpComparer)
                .ToArray();
            var allProperties = dynamicProperties.Concat(
                baseType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null && p.GetCustomAttribute<NonCompositeAttribute>() == null)
                .Select(p => new DynamicProperty(p.Name, p.PropertyType)))
                .ToList();
            var interfaces =
                derivedTypes
                .SelectMany(t => t.GetInterfaces())   // We only want method-free interfaces
                .Distinct(typeComparer)
                .Where(i => i.GetMethods().All(mi => mi.IsSpecialName) && i.GetProperties().All(p => allProperties.Any(dp => dp.Name == p.Name)))
                .ToArray();
            Type composite = GetCompositeClass(typeBuilders, baseType, dynamicProperties, interfaces);
            return composite;
        }

        /// <summary>
        /// Dynamically generate a composite class which inherits from a base type and includes all the properties from a
        /// list of interfaces, and which implements those interfaces
        /// </summary>
        /// <param name="baseType">The base type</param>
        /// <param name="interfaces">List of interfaces</param>
        /// <returns>Dynamically created composite of the base type and interfaces</returns>
        public Type GetCompositeClassByInterfaces(Dictionary<Type, TypeBuilder> typeBuilders, Type baseType, List<Type> interfaces)
        {
            if (baseType.IsSealed() || baseType.GetCustomAttribute<NonCompositeAttribute>() != null || interfaces.Count == 0)
                return baseType;

            var dpComparer = new StringSelectorComparer<DynamicProperty>(dp => dp.Name, false);

            var dynamicProperties =
                interfaces
                .Recurse(i => i.GetInterfaces()) // GetProperties only return properties on top level interface type
                .SelectMany(t => t
                    .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null && p.GetCustomAttribute<NonCompositeAttribute>() == null)
                    .Select(p => new DynamicProperty(p.Name, p.PropertyType)))
                .Distinct(dpComparer)
                .ToArray();
            var allProperties = dynamicProperties.Concat(
                baseType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null && p.GetCustomAttribute<NonCompositeAttribute>() == null)
                .Select(p => new DynamicProperty(p.Name, p.PropertyType)))
                .ToList();
            Type composite = GetCompositeClass(typeBuilders, baseType, dynamicProperties, interfaces.ToArray());
            return composite;
        }

        /// <summary>
        /// Gets a summarised type from a composite class which is just a type inheriting from object containing only the fields
        /// in the composite class needed to create a summary
        /// </summary>
        /// <param name="extendedType">The composite class</param>
        /// <returns>The summarised type</returns>
        public Type GetSummarisedType(Collator coll, Dictionary<Type, TypeBuilder> typeBuilders, Type extendedType)
        {
            var dynamicProperties = coll.ContainerSummaryFields(extendedType)
                .Select(p => new DynamicProperty(p.Name, p.PropertyType))
                .ToArray();
            return GetCompositeClass(typeBuilders, typeof(object), dynamicProperties);
        }

        /// <summary>
        /// Get a composite class from the base type and all the properties needed
        /// </summary>
        /// <param name="baseType">Base type</param>
        /// <param name="properties">properties needed</param>
        /// <returns>Dynamically built composite type</returns>
        public Type GetCompositeClass(Dictionary<Type, TypeBuilder> typeBuilders, Type baseType, IEnumerable<DynamicProperty> properties)
        {
            return GetCompositeClass(typeBuilders, baseType, properties, null);
        }
        /// <summary>
        /// Get a composite class from the base type, all the properties and interfaces needed
        /// </summary>
        /// <param name="baseType">Base type</param>
        /// <param name="properties">properties needed</param>
        /// <param name="interfaces">interfaces needed</param>
        /// <returns>Dynamically built composite type</returns>
        public Type GetCompositeClass(Dictionary<Type, TypeBuilder> typeBuilders, Type baseType, IEnumerable<DynamicProperty> properties, IEnumerable<Type> interfaces)
        {
            rwLock.EnterUpgradeableReadLock();
            try
            {
                Signature signature = new Signature(properties);
                Type type;
                //if (!classes.TryGetValue(signature, out type))
                //{
                    type = CreateDynamicClass(typeBuilders, baseType, signature.properties, interfaces);
                //    classes.Add(signature, type);
                //}
                return type;
            }
            finally
            {
                rwLock.ExitUpgradeableReadLock();
            }
        }

        Type CreateDynamicClass(Dictionary<Type, TypeBuilder> typeBuilders, Type baseType, DynamicProperty[] properties, IEnumerable<Type> interfaces)
        {
            rwLock.EnterWriteLock();
            try
            {
                string typeName = "Composite" + classCount + "_" + baseType.Name;
#if ENABLE_LINQ_PARTIAL_TRUST
                new ReflectionPermission(PermissionState.Unrestricted).Assert();
#endif
                try
                {
                    TypeBuilder tb;
                    if (baseType != null && typeBuilders.ContainsKey(baseType))
                        tb = typeBuilders[baseType];
                    else
                        tb = this.module.DefineType(typeName, TypeAttributes.Class | TypeAttributes.Public, baseType ?? typeof(object));

                    if (interfaces != null)
                        foreach (Type iType in interfaces)
                            tb.AddInterfaceImplementation(iType);
                    tb.SetCustomAttribute(new CustomAttributeBuilder(typeof(SerializableAttribute).GetConstructor(Type.EmptyTypes), new object[] { }));
                    FieldInfo[] fields = GenerateProperties(tb, properties, interfaces);
                    GenerateEquals(tb, fields);
                    GenerateGetHashCode(tb, fields);
                    Type result = tb.CreateTypeInfo().AsType();
                    classCount++;
                    return result;
                }
                finally
                {
#if ENABLE_LINQ_PARTIAL_TRUST
                    PermissionSet.RevertAssert();
#endif
                }
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        FieldInfo[] GenerateProperties(TypeBuilder tb, DynamicProperty[] properties)
        {
            return GenerateProperties(tb, properties, new List<Type>());
        }
        FieldInfo[] GenerateProperties(TypeBuilder tb, DynamicProperty[] properties, IEnumerable<Type> interfaces)
        {
            FieldInfo[] fields = new FieldBuilder[properties.Length];
            for (int i = 0; i < properties.Length; i++)
            {
                DynamicProperty dp = properties[i];
                FieldBuilder fb = tb.DefineField("_" + dp.Name, dp.Type, FieldAttributes.Private);
                PropertyBuilder pb = tb.DefineProperty(dp.Name, PropertyAttributes.HasDefault, dp.Type, null);
                MethodBuilder mbGet = tb.DefineMethod("get_" + dp.Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                    dp.Type, Type.EmptyTypes);
                ILGenerator genGet = mbGet.GetILGenerator();
                genGet.Emit(OpCodes.Ldarg_0);
                genGet.Emit(OpCodes.Ldfld, fb);
                genGet.Emit(OpCodes.Ret);
                MethodBuilder mbSet = tb.DefineMethod("set_" + dp.Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                    null, new Type[] { dp.Type });
                ILGenerator genSet = mbSet.GetILGenerator();
                genSet.Emit(OpCodes.Ldarg_0);
                genSet.Emit(OpCodes.Ldarg_1);
                genSet.Emit(OpCodes.Stfld, fb);
                genSet.Emit(OpCodes.Ret);
                pb.SetGetMethod(mbGet);
                pb.SetSetMethod(mbSet);
                fields[i] = fb;

                if (interfaces != null)
                {
                    // If this property belongs to an interface set it up as implementing that interface
                    var implProperties = interfaces.Select(itf => itf.GetProperty(dp.Name)).Where(pi => pi != null).ToList();
                    foreach (var implProp in implProperties)
                    {
                        foreach (var attr in CustomAttributeData.GetCustomAttributes(implProp))
                        {
                            pb.SetCustomAttribute(ToAttributeBuilder(attr));
                        }
                        MethodInfo miGet = implProp.GetGetMethod();
                        if (miGet != null)
                            tb.DefineMethodOverride(mbGet, miGet);
                        MethodInfo miSet = implProp.GetSetMethod();
                        if (miSet != null)
                            tb.DefineMethodOverride(mbSet, miSet);
                    }
                }
            }
            return fields;
        }

        public CustomAttributeBuilder ToAttributeBuilder(CustomAttributeData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            var constructorArguments = new List<object>();
            foreach (var ctorArg in data.ConstructorArguments)
            {
                constructorArguments.Add(ctorArg.Value);
            }

            var propertyArguments = new List<PropertyInfo>();
            var propertyArgumentValues = new List<object>();
            var fieldArguments = new List<FieldInfo>();
            var fieldArgumentValues = new List<object>();
            foreach (var namedArg in data.NamedArguments)
            {
                string argName = namedArg.MemberName;
                var fi = data.AttributeType.GetField(argName);
                var pi = data.AttributeType.GetProperty(argName);

                if (fi != null)
                {
                    fieldArguments.Add(fi);
                    fieldArgumentValues.Add(namedArg.TypedValue.Value);
                }
                else if (pi != null)
                {
                    propertyArguments.Add(pi);
                    propertyArgumentValues.Add(namedArg.TypedValue.Value);
                }
            }
            return new CustomAttributeBuilder(
              data.Constructor,
              constructorArguments.ToArray(),
              propertyArguments.ToArray(),
              propertyArgumentValues.ToArray(),
              fieldArguments.ToArray(),
              fieldArgumentValues.ToArray());
        }

        void GenerateEquals(TypeBuilder tb, FieldInfo[] fields)
        {
            MethodBuilder mb = tb.DefineMethod("Equals",
                MethodAttributes.Public | MethodAttributes.ReuseSlot |
                MethodAttributes.Virtual | MethodAttributes.HideBySig,
                typeof(bool), new Type[] { typeof(object) });
            ILGenerator gen = mb.GetILGenerator();
            LocalBuilder other = gen.DeclareLocal(tb.AsType());
            Label next = gen.DefineLabel();
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Isinst, tb.AsType());
            gen.Emit(OpCodes.Stloc, other);
            gen.Emit(OpCodes.Ldloc, other);
            gen.Emit(OpCodes.Brtrue_S, next);
            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Ret);
            gen.MarkLabel(next);
            foreach (FieldInfo field in fields)
            {
                Type ft = field.FieldType;
                Type ct = typeof(EqualityComparer<>).MakeGenericType(ft);
                next = gen.DefineLabel();
                gen.EmitCall(OpCodes.Call, ct.GetMethod("get_Default"), null);
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, field);
                gen.Emit(OpCodes.Ldloc, other);
                gen.Emit(OpCodes.Ldfld, field);
                gen.EmitCall(OpCodes.Callvirt, ct.GetMethod("Equals", new Type[] { ft, ft }), null);
                gen.Emit(OpCodes.Brtrue_S, next);
                gen.Emit(OpCodes.Ldc_I4_0);
                gen.Emit(OpCodes.Ret);
                gen.MarkLabel(next);
            }
            gen.Emit(OpCodes.Ldc_I4_1);
            gen.Emit(OpCodes.Ret);
        }

        void GenerateGetHashCode(TypeBuilder tb, FieldInfo[] fields)
        {
            MethodBuilder mb = tb.DefineMethod("GetHashCode",
                MethodAttributes.Public | MethodAttributes.ReuseSlot |
                MethodAttributes.Virtual | MethodAttributes.HideBySig,
                typeof(int), Type.EmptyTypes);
            ILGenerator gen = mb.GetILGenerator();
            gen.Emit(OpCodes.Ldc_I4_0);
            foreach (FieldInfo field in fields)
            {
                Type ft = field.FieldType;
                Type ct = typeof(EqualityComparer<>).MakeGenericType(ft);
                gen.EmitCall(OpCodes.Call, ct.GetMethod("get_Default"), null);
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, field);
                gen.EmitCall(OpCodes.Callvirt, ct.GetMethod("GetHashCode", new Type[] { ft }), null);
                gen.Emit(OpCodes.Xor);
            }
            gen.Emit(OpCodes.Ret);
        }
    }
}
