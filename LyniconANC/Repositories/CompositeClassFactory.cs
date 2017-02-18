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

namespace Lynicon.Repositories
{
    /// <summary>
    /// Copied and extended from Linq.Dynamic.ClassFactory
    /// </summary>
    internal class CompositeClassFactory
    {
        public static readonly CompositeClassFactory Instance = new CompositeClassFactory();

        static CompositeClassFactory() { }  // Trigger lazy initialization of static fields

        ModuleBuilder module;
        //Dictionary<Signature, Type> classes;
        int classCount;
        ReaderWriterLock rwLock;

        Dictionary<Type, TypeBuilder> typeBuilders = new Dictionary<Type,TypeBuilder>();

        private CompositeClassFactory()
        {
            AssemblyName name = new AssemblyName("CompositeClasses");
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
            rwLock = new ReaderWriterLock();
        }

        /// <summary>
        /// Initialise the composite class factory
        /// </summary>
        /// <param name="baseTypes">the base types to build composites for</param>
        public void Initialise(List<Type> baseTypes)
        {
            rwLock.AcquireWriterLock(Timeout.Infinite);

            try
            {
                int classCount = 0;
                foreach (var baseType in baseTypes)
                {
                    if (baseType.IsSealed || baseType.GetCustomAttribute<NonCompositeAttribute>() != null)
                        continue;
                    string typeName = "Composite_" + classCount.ToString() + baseType.Name;
                    TypeBuilder tb = this.module.DefineType(typeName, TypeAttributes.Class |
                            TypeAttributes.Public, baseType);
                    typeBuilders.Add(baseType, tb);
                    classCount++;
                }
            }
            finally
            {
                rwLock.ReleaseWriterLock();
            }
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
            if (baseType.IsSealed || baseType.GetCustomAttribute<NonCompositeAttribute>() != null || derivedTypes.Count == 0)
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
            Type composite = GetCompositeClass(baseType, dynamicProperties, interfaces);
            return composite;
        }

        /// <summary>
        /// Gets a summarised type from a composite class which is just a type inheriting from object containing only the fields
        /// in the composite class needed to create a summary
        /// </summary>
        /// <param name="extendedType">The composite class</param>
        /// <returns>The summarised type</returns>
        public Type GetSummarisedType(Type extendedType)
        {
            var dynamicProperties = Collator.Instance.ContainerSummaryFields(extendedType)
                .Select(p => new DynamicProperty(p.Name, p.PropertyType))
                .ToArray();
            return GetCompositeClass(typeof(object), dynamicProperties);
        }

        /// <summary>
        /// Get a composite class from the base type and all the properties needed
        /// </summary>
        /// <param name="baseType">Base type</param>
        /// <param name="properties">properties needed</param>
        /// <returns>Dynamically built composite type</returns>
        public Type GetCompositeClass(Type baseType, IEnumerable<DynamicProperty> properties)
        {
            return GetCompositeClass(baseType, properties, null);
        }
        /// <summary>
        /// Get a composite class from the base type, all the properties and interfaces needed
        /// </summary>
        /// <param name="baseType">Base type</param>
        /// <param name="properties">properties needed</param>
        /// <param name="interfaces">interfaces needed</param>
        /// <returns>Dynamically built composite type</returns>
        public Type GetCompositeClass(Type baseType, IEnumerable<DynamicProperty> properties, IEnumerable<Type> interfaces)
        {
            rwLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                Signature signature = new Signature(properties);
                Type type;
                //if (!classes.TryGetValue(signature, out type))
                //{
                    type = CreateDynamicClass(baseType, signature.properties, interfaces);
                //    classes.Add(signature, type);
                //}
                return type;
            }
            finally
            {
                rwLock.ReleaseReaderLock();
            }
        }

        Type CreateDynamicClass(Type baseType, DynamicProperty[] properties, IEnumerable<Type> interfaces)
        {
            LockCookie cookie = rwLock.UpgradeToWriterLock(Timeout.Infinite);
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
                    Type result = tb.CreateTypeInfo();
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
                rwLock.DowngradeFromWriterLock(ref cookie);
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
                    // Set up implementation of interfaces
                    var implProperties = interfaces.Select(itf => itf.GetProperty(dp.Name)).Where(pi => pi != null).ToList();
                    foreach (var implProp in implProperties)
                    {
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

        void GenerateEquals(TypeBuilder tb, FieldInfo[] fields)
        {
            MethodBuilder mb = tb.DefineMethod("Equals",
                MethodAttributes.Public | MethodAttributes.ReuseSlot |
                MethodAttributes.Virtual | MethodAttributes.HideBySig,
                typeof(bool), new Type[] { typeof(object) });
            ILGenerator gen = mb.GetILGenerator();
            LocalBuilder other = gen.DeclareLocal(tb);
            Label next = gen.DefineLabel();
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Isinst, tb);
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
