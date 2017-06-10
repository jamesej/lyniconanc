using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lynicon.Repositories;
using System.Collections;
using Newtonsoft.Json;
using Lynicon.Collation;
using Lynicon.Relations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using LyniconANC.Extensibility;

namespace Lynicon.Utility
{
    public class ObjectPropertyInfo
    {
        public PropertyInfo Prop { get; set; }
        public int? Index { get; set; }
        public object Source { get; set;}
    }

    /// <summary>
    /// Utility methods and extension methods for reflection
    /// </summary>
    public static class ReflectionX
    {
        public static bool IsValueType(this Type type) => type.GetTypeInfo().IsValueType;
        public static bool IsGenericType(this Type type) => type.GetTypeInfo().IsGenericType;
        public static bool IsInterface(this Type type) => type.GetTypeInfo().IsInterface;
        public static bool IsEnum(this Type type) => type.GetTypeInfo().IsEnum;
        public static bool IsClass(this Type type) => type.GetTypeInfo().IsClass;
        public static bool IsSealed(this Type type) => type.GetTypeInfo().IsSealed;
        public static Type BaseType(this Type type) => type.GetTypeInfo().BaseType;
        public static T GetCustomAttribute<T>(this Type type) where T : Attribute => (T)type.GetCustomAttributes(typeof(T), true).FirstOrDefault();
        public static MemberInfo[] FindMembers(this Type type, MemberTypes memberType, BindingFlags bindingAttr, MemberFilter filter, object filterCriteria) => type.GetTypeInfo().FindMembers(memberType, bindingAttr, filter, filterCriteria);
        public static MemberFilter FilterNameIgnoreCase = (mem, name) => mem.Name.ToLower() == name.ToString().ToLower();
        public static TypeAttributes Attributes(this Type t) => t.GetTypeInfo().Attributes;
        public static IEnumerable<Attribute> GetCustomAttributes(this Type t, Type attributeType, bool inherit) => t.GetTypeInfo().GetCustomAttributes(attributeType, inherit).Cast<Attribute>().ToList();
        public static IEnumerable<Attribute> GetCustomAttributes(this Type t, bool inherit) => t.GetCustomAttributes(inherit).Cast<Attribute>().ToList();
        public static Type[] FindInterfaces(this Type t, TypeFilter filter, object filterCriteria) => t.GetTypeInfo().FindInterfaces(filter, filterCriteria);
        public static IList<CustomAttributeData> GetCustomAttributesData(this Type t) => CustomAttributeData.GetCustomAttributes(t.GetTypeInfo());
        public static IList<CustomAttributeData> GetCustomAttributesData(this MemberInfo mi) => CustomAttributeData.GetCustomAttributes(mi);
        public static bool IsSubclassOf(this Type t, Type type) => t.GetTypeInfo().IsSubclassOf(type);
        public static bool IsPrimitive(this Type t) => t.GetTypeInfo().IsPrimitive;
        public static Type GetInterface(this Type t, string name) => t.GetTypeInfo().GetInterface(name);


        /// <summary>
        /// Change the type of an object to a given type
        /// </summary>
        /// <param name="o">The object</param>
        /// <param name="t">The type to convert it to</param>
        /// <returns>Converted object</returns>
        public static object ChangeType(object o, Type t)
        {
            if (o is string && t == typeof(Guid))
                return new Guid((string)o);
            else
                return Convert.ChangeType(o, t);
        }

        /// <summary>
        /// Take a type and substitute any inner generic argument or the whole type matching one type for another type
        /// </summary>
        /// <param name="type">The type to convert wholly or partially</param>
        /// <param name="from">The type which is converted wherever it is mentioned</param>
        /// <param name="to">The type it is converted to</param>
        /// <returns>The converted type</returns>
        public static Type SubstituteType(Type type, Type from, Type to)
        {
            var types = SubstituteTypes(new Type[] { type }, from, to);
            if (types == null || types.Length == 0)
                return null;
            else
                return types[0];
        }
        /// <summary>
        /// Take a list of types and substitute any inner generic argument or the whole type matching one type for another type
        /// </summary>
        /// <param name="type">The types to convert wholly or partially</param>
        /// <param name="from">The type which is converted wherever it is mentioned</param>
        /// <param name="to">The type it is converted to</param>
        /// <returns>The converted types</returns>
        public static Type[] SubstituteTypes(Type[] types, Type from, Type to)
        {
            bool isChanged = false;
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == from)
                {
                    types[i] = to;
                    isChanged = true;
                }
                else if (types[i].IsGenericType())
                {
                    Type[] typeArgs = SubstituteTypes(types[i].GenericTypeArguments, from, to);
                    if (typeArgs != null)
                    {
                        isChanged = true;
                        types[i] = types[i]
                            .GetGenericTypeDefinition()
                            .MakeGenericType(typeArgs);
                    }
                }
            }
            if (isChanged)
                return types;
            else
                return null;
        }

        /// <summary>
        /// Get property value from an object by a property path (e.g. x.y.z)
        /// </summary>
        /// <param name="o">Source of property</param>
        /// <param name="path">The property path (e.g. x.y.z)</param>
        /// <returns>The property value</returns>
        public static object GetPropertyValueByPath(object o, string path)
        {
            return GetPropertyValueByPath(o, path, false);
        }
        /// <summary>
        /// Get property value from an object by a property path (e.g. x.y.z)
        /// </summary>
        /// <param name="o">Source of property</param>
        /// <param name="path">The property path (e.g. x.y.z)</param>
        /// <param name="create">If true, create any missing parent property values</param>
        /// <returns>The property value</returns>
        public static object GetPropertyValueByPath(object o, string path, bool create)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            string[] pathEls = path.Split('.');
            Type propType = o.GetType();
            PropertyInfo propInfo = null;
            object val = o;
            foreach (string pathEl in pathEls)
            {
                propInfo = propType.GetProperty(pathEl.UpTo("["));
                object newVal = propInfo.GetValue(val, null);
                if (create && newVal == null)
                {
                    newVal = Activator.CreateInstance(propInfo.PropertyType);
                    propInfo.SetValue(val, newVal);
                }
                else if (newVal == null)
                    return null;
                val = newVal;
                if (pathEl.Contains("["))
                {
                    int idx = int.Parse(pathEl.After("[").UpTo("]"));
                    var countPi = propInfo.PropertyType.GetProperty("Count");
                    var addMi = propInfo.PropertyType.GetMethod("Add");
                    propInfo = propInfo.PropertyType.GetProperty("Item");
                    // add new items if they're missing
                    if (create && countPi != null && addMi != null)
                        for (int i = (int)countPi.GetValue(val); i <= idx; i++)
                            addMi.Invoke(val, new object[] { Activator.CreateInstance(propInfo.PropertyType) });

                    object[] indexes = new object[] { idx };
                    val = propInfo.GetValue(val, indexes);
                }
                propType = propInfo.PropertyType;
            }
            return val;
        }

        /// <summary>
        /// Get a property info by path (e.g. x.y.z)
        /// </summary>
        /// <param name="t">Type to get property info from</param>
        /// <param name="path">Path of property (e.g. x.y.z)</param>
        /// <returns>the PropertyInfo</returns>
        public static PropertyInfo GetPropertyByPath(Type t, string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            string[] pathEls = path.Split('.');
            Type propType = t;
            PropertyInfo propInfo = null;
            foreach (string pathEl in pathEls)
            {
                propInfo = propType.GetProperty(pathEl.UpTo("["));
                if (pathEl.Contains("["))
                    propInfo = propInfo.PropertyType.GetProperty("Item");
                propType = propInfo.PropertyType;
            }
            return propInfo;
        }

        /// <summary>
        /// Get an expression for accessing a member of a type at a given property path
        /// </summary>
        /// <param name="t">The type to get the member access expression for</param>
        /// <param name="exp">The expression on which the member access is performed in the output expression</param>
        /// <param name="path">The property path to the property of the type (e.g. x.y.z)</param>
        /// <returns>An expression for a member access on the given expression of given type to access the property on the given path</returns>
        public static Expression GetMemberAccessByPath(Type t, Expression exp, string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Can't create a member access for a null path");

            string[] pathEls = path.Split('.');
            Type propType = t;
            PropertyInfo propInfo = null;
            Expression propExp = exp;
            foreach (string pathEl in pathEls)
            {
                propInfo = propType.GetProperty(pathEl.UpTo("["));
                propExp = Expression.MakeMemberAccess(propExp, propInfo);
                propType = propInfo.PropertyType;

                if (pathEl.Contains("["))
                {
                    int idx = int.Parse(pathEl.After("[").UpTo("]"));
                    ConstantExpression indexExp = Expression.Constant(idx);
                    propExp = Expression.ArrayAccess(propExp, indexExp);
                }
            }

            return propExp;
        }

        /// <summary>
        /// Get the type of a property at a given property path on a given type
        /// </summary>
        /// <param name="parent">The type to start from</param>
        /// <param name="path">The path to follow to the property (e.g. x.y.z)</param>
        /// <returns>The type of the property</returns>
        public static Type GetPropertyTypeByPath(Type parent, string path)
        {
            if (string.IsNullOrEmpty(path))
                return parent;

            string[] pathEls = path.Split('.');
            Type t = parent;
            foreach (string pathEl in pathEls)
            {
                t = t.GetProperty(pathEl.UpTo("[")).PropertyType;
                if (pathEl.Contains("["))
                    t = t.GetProperty("Item").PropertyType;
            }
            return t;
        }

        /// <summary>
        /// Scan an object for properties labelled with an attribute of a given type
        /// </summary>
        /// <typeparam name="T">The type of the attribute to search for</typeparam>
        /// <param name="o">The object</param>
        /// <returns>Tuple of the parent object, the property of it, and the attribute attached to the property for all cases of properties having the required attribute</returns>
        public static IEnumerable<Tuple<object, PropertyInfo, T>> GetPropertiesWithAttributesRecursive<T>(object o)
            where T : Attribute
        {
            Type type = o.GetType();
            foreach (var pi in type.GetProperties())
            {
                T attr = type.GetCustomAttribute<T>();
                if (attr != null)
                    yield return new Tuple<object, PropertyInfo, T>(o, pi, attr);
                if (pi.PropertyType.IsClass())
                {
                    object val = pi.GetValue(o);
                    if (val != null)
                        foreach (var res in GetPropertiesWithAttributesRecursive<T>(val))
                            yield return res;
                }
            }
        }

        /// <summary>
        /// Get the default value for a type at runtime
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The default value</returns>
        public static object GetDefault(Type type)
        {
            if (type == typeof(string))
                return String.Empty;
            if (type == typeof(Guid))
                return Guid.Empty;

            if (type.IsValueType())
                return Activator.CreateInstance(type);

            return null;
        }

        /// <summary>
        /// Get a default value for a value type, or a new instance for a reference type
        /// </summary>
        /// <param name="type">The type to get a value for</param>
        /// <returns>The default/new value</returns>
        public static object NewOrDefault(Type type)
        {
            if (type == typeof(string))
                return String.Empty;

            if (type.IsValueType())
                return GetDefault(type);
            else
                return Activator.CreateInstance(type);
        }

        /// <summary>
        /// Call a generic method specifying type and arguments at runtime
        /// </summary>
        /// <param name="instance">The object on which to call the method, or null if static</param>
        /// <param name="method">The name of the method</param>
        /// <param name="type0">The type of the single generic type argument</param>
        /// <param name="args">The method parameters</param>
        /// <returns>The result of calling the method</returns>
        public static object InvokeGenericMethod(object instance, string method, Type type0, params object[] args)
        {
            return InvokeGenericMethod(instance, method, mi => true, new[] { type0 }, args);
        }
        /// <summary>
        /// Call a generic method specifying type, visibility and arguments at runtime
        /// </summary>
        /// <param name="instance">The object on which to call the method, or null if static</param>
        /// <param name="method">The name of the method</param>
        /// <param name="type0">The type of the single generic type argument</param>
        /// <param name="isPublic">Whether the method to call is public</param>
        /// <param name="args">The method parameters</param>
        /// <returns>The result of calling the method</returns>
        public static object InvokeGenericMethod(object instance, string method, Type type0, bool isPublic, params object[] args)
        {
            return InvokeGenericMethod(instance, method, isPublic, mi => true, new[] { type0 }, args);
        }
        /// <summary>
        /// Call a generic method specifying type and arguments at runtime
        /// </summary>
        /// <param name="instance">The object on which to call the method, or null if static</param>
        /// <param name="method">The name of the method</param>
        /// <param name="types">List of the types of the generic type arguments</param>
        /// <param name="args">The method parameters</param>
        /// <returns>The result of calling the method</returns>
        public static object InvokeGenericMethod(object instance, string method, IEnumerable<Type> types, params object[] args)
        {
            return InvokeGenericMethod(instance, method, mi => true, types, args);
        }
        /// <summary>
        /// Call a generic method specifying type and arguments at runtime
        /// </summary>
        /// <param name="instance">The object on which to call the method, or null if static</param>
        /// <param name="method">The name of the method</param>
        /// <param name="types">List of the types of the generic type arguments</param>
        /// <param name="args">The method parameters</param>
        /// <returns>The result of calling the method</returns>
        public static object InvokeGenericMethod(object instance, string method, Func<MethodInfo, bool> methodSelector, IEnumerable<Type> types, params object[] args)
        {
            return InvokeGenericMethod(instance, method, true, methodSelector, types, args);
        }
        /// <summary>
        /// Call a generic method specifying type and arguments at runtime
        /// </summary>
        /// <param name="instance">The object on which to call the method, or null if static</param>
        /// <param name="method">The name of the method</param>
        /// <param name="isPublic">Whether the method to call is public</param>
        /// <param name="methodSelector">Code to test the candidate methods, returns true if this method is to be used</param>
        /// <param name="types">List of the types of the generic type arguments</param>
        /// <param name="args">The method parameters</param>
        /// <returns>The result of calling the method</returns>
        public static object InvokeGenericMethod(object instance, string method, bool isPublic, Func<MethodInfo, bool> methodSelector, IEnumerable<Type> types, params object[] args)
        {
            Type t = instance.GetType();
            return t.GetMethods((isPublic ? BindingFlags.Public : BindingFlags.NonPublic) | BindingFlags.Instance)
                .Where(mi => mi.Name == method && mi.GetGenericArguments().Count() == types.Count() && mi.GetParameters().Length == args.Length)
                .Single(methodSelector)
                .MakeGenericMethod(types.ToArray()).Invoke(instance, args);
        }
        /// <summary>
        /// Call a generic extension method specifying type and arguments at runtime
        /// </summary>
        /// <param name="extensionMethodSource">The type containing the extension method</param>
        /// <param name="method">The name of the method</param>
        /// <param name="types">List of the types of the generic type arguments</param>
        /// <param name="args">The method parameters</param>
        /// <returns>The result of calling the method</returns>
        public static object InvokeGenericMethod(Type extensionMethodSource, string method, IEnumerable<Type> types, params object[] args)
        {
            return InvokeGenericMethod(extensionMethodSource, method, mi => true, types, args);
        }
        /// <summary>
        /// Call a generic extension method specifying type and arguments at runtime
        /// </summary>
        /// <param name="extensionMethodSource">The type containing the extension method</param>
        /// <param name="method">The name of the method</param>
        /// <param name="methodSelector">Code to test the candidate methods, returns true if this method is to be used</param>
        /// <param name="types">List of the types of the generic type arguments</param>
        /// <param name="args">The method parameters</param>
        /// <returns>The result of calling the method</returns>
        public static object InvokeGenericMethod(Type extensionMethodSource, string method, Func<MethodInfo, bool> methodSelector, IEnumerable<Type> types, params object[] args)
        {
            // this breaks with a null arg
            //var argTypes = args.Select(a => a.GetType()).ToArray();
            return extensionMethodSource
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(mi => mi.Name == method && mi.GetGenericArguments().Count() == types.Count() && mi.GetParameters().Length == args.Length)
                .Single(methodSelector)
                .MakeGenericMethod(types.ToArray()).Invoke(null, args);

        }

        /// <summary>
        /// Create a copy of an arbitrary entity by just copying the properties which would be persisted to a data source
        /// </summary>
        /// <param name="o">The entity to copy</param>
        /// <returns>The copy</returns>
        public static object CopyEntity(object o)
        {
            Type t = o.GetType().UnproxiedType();
            object copy = Activator.CreateInstance(t);
            foreach (PropertyInfo pi in t.GetPersistedProperties())
                pi.SetValue(copy, pi.GetValue(o));

            return copy;
        }
        /// <summary>
        /// Create a copy of an arbitrary entity by just copying the properties which would be persisted to a data source
        /// </summary>
        /// <typeparam name="T">The type of the entity</typeparam>
        /// <param name="o">The entity to copy</param>
        /// <returns>The copy</returns>
        public static T CopyEntity<T>(object o) where T: class
        {
            Type t = o.GetType().UnproxiedType();
            T copy = Activator.CreateInstance<T>();
            foreach (PropertyInfo piTo in typeof(T).GetPersistedProperties())
            {
                PropertyInfo piFrom = t.GetProperty(piTo.Name);
                piTo.SetValue(copy, piFrom.GetValue(o));
            }

            return copy;
        }

        /// <summary>
        /// Get the properties of a type which would be persisted to a data source
        /// </summary>
        /// <param name="t">The type</param>
        /// <returns>Array of property infos of the persisted properties</returns>
        public static PropertyInfo[] GetPersistedProperties(this Type t)
        {
            return t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(pi => pi.CanRead && pi.CanWrite && pi.GetCustomAttribute<NotMappedAttribute>() == null && pi.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                    .ToArray();
        }

        /// <summary>
        /// If a type is an Entity Framework proxy type, return the type it is a proxy of.
        /// Otherwise just return the type
        /// </summary>
        /// <param name="t">The type</param>
        /// <returns>The type removing any proxy</returns>
        public static Type UnproxiedType(this Type t)
        {
            if (t.Namespace == "System.Data.Entity.DynamicProxies")
                return t.BaseType();
            else
                return t;
        }

        /// <summary>
        /// Get the (unextended) container type associated with a type
        /// </summary>
        /// <param name="t">The type</param>
        /// <returns>The associated unextended type</returns>
        public static Type UnextendedType(this Type t)
        {
            Type res = t;
            if (res.IsGenericType())
                res = res.GetGenericArguments().First();
            res = res.UnproxiedType();
            res = TypeExtender.BaseType(res);
            return res;
        }

        /// <summary>
        /// Get from a type the associated generic interface matching an unresolved generic interface type (e.g. IEnumerable<>)
        /// </summary>
        /// <param name="queryType">The type</param>
        /// <param name="interfaceType">The unresolved generic interface type (e.g. IEnumerable<>)</param>
        /// <returns>The resolved generic interface type</returns>
        public static Type ExtractGenericInterface(this Type queryType, Type interfaceType)
        {
            Func<Type, bool> matchesInterface = t => t.IsGenericType() && t.GetGenericTypeDefinition() == interfaceType;
            return (matchesInterface(queryType)) ? queryType : queryType.GetInterfaces().FirstOrDefault(matchesInterface);
        }

        /// <summary>
        /// Get the element type from a list
        /// </summary>
        /// <param name="iList">The list</param>
        /// <returns>The element type</returns>
        public static Type ElementType(IList iList)
        {
            return ElementType(iList.GetType());
        }
        /// <summary>
        /// Get the element type from an aggregate type
        /// </summary>
        /// <param name="t">aggregate type</param>
        /// <returns>element type</returns>
        public static Type ElementType(Type t)
        {
            if (t.IsArray)
                return t.GetElementType();
            else
            {
                Type tBase = t;
                while (!tBase.IsGenericType() && tBase.BaseType() != null)
                    tBase = tBase.BaseType();
                return tBase.GetGenericArguments()[0];
            }
                
        }

        /// <summary>
        /// Find the best possibility of the title of a content item
        /// </summary>
        /// <param name="o">content item</param>
        /// <returns>Title of the content item</returns>
        public static string TryGetTitle(object o)
        {
            var tpi = o.GetType().GetProperty("Title");
            if (tpi == null)
                tpi = o.GetType().GetProperty("Name");
            if (tpi == null)
                return null;
            else
            {
                var val = tpi.GetValue(o);
                if (val == null)
                    return null;
                else
                    return val.ToString();
            }
        }

        /// <summary>
        /// Traverse recursively the values of all properties and subproperties on an object, providing an ObjectPropertyInfo for each,
        /// using a filter
        /// </summary>
        /// <param name="o">The parent object</param>
        /// <param name="exclude">Test for excluding a property from being searched</param>
        /// <returns>Enumerable of ObjectPropertyInfos for all unfiltered property values</returns>
        public static IEnumerable<ObjectPropertyInfo> GetObjectPropertyInfos(object o, Func<PropertyInfo, bool> exclude)
        {
            return GetObjectPropertyInfos(o, null, 0, exclude);
        }
        /// <summary>
        /// Traverse recursively the values of all properties and subproperties on an object, providing an ObjectPropertyInfo for each,
        /// using a filter
        /// </summary>
        /// <param name="o">The parent object</param>
        /// <param name="index">The index of the item in the IList that contains it, if any</param>
        /// <param name="depth">The recursion depth</param>
        /// <param name="exclude">Test for excluding a property from being searched</param>
        /// <returns>Enumerable of ObjectPropertyInfos for all unfiltered property values</returns>
        public static IEnumerable<ObjectPropertyInfo> GetObjectPropertyInfos(object o, int? index, int depth, Func<PropertyInfo, bool> exclude)
        {
            if (o == null)
                yield break;

            if (depth > 100)
                throw new Exception("GetObjectPropertyInfo exceeded allowed recursion level");

            foreach (var pi in o.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty))
            {
                if (exclude(pi) || pi.GetIndexParameters().Length > 0)
                    continue;

                object val = pi.GetValue(o);
                if (val == null)
                    continue;

                Type t = pi.PropertyType;
                if (t.GetInterface("IList") != null)
                {
                    yield return new ObjectPropertyInfo { Index = index, Prop = pi, Source = o };
                    int idx = 0;
                    foreach (var item in ((IList)val))
                    {
                        foreach (var opi in GetObjectPropertyInfos(item, idx, depth + 1, exclude))
                            yield return opi;
                        idx++;
                    }
                }
                else if (t.IsPrimitive() || t == typeof(string) || t == typeof(DateTime) || t == typeof(ItemId) || t == typeof(Type) || t == typeof(Reference) || t.BaseType() == typeof(Reference))
                {
                    yield return new ObjectPropertyInfo { Index = index, Prop = pi, Source = o };
                }
                else if ((t.IsClass() && pi.GetIndexParameters().Length == 0))
                {
                    yield return new ObjectPropertyInfo { Index = index, Prop = pi, Source = o };
                    foreach (var opi in GetObjectPropertyInfos(val, null, depth + 1, exclude))
                        yield return opi;
                }
            }
        }

        public static ConstructorInfo GetConstructorByNonServiceParams(this Type type, Type[] nonServiceParamTypes)
        {
            var anyCons = type.GetConstructors().First();
            var serviceParams = anyCons.GetParameters().Where(p => p.GetCustomAttribute<FromServicesAttribute>() != null);
            var allParamTypes = serviceParams.Select(p => p.ParameterType).Concat(nonServiceParamTypes).ToArray();
            var matchedCons = type.GetConstructor(allParamTypes);
            if (matchedCons == null)
            {
                // try and find a constructor with final 'params' parameter
                foreach (var cons in type.GetConstructors())
                {
                    var nParms = cons.GetParameters().Length;
                    var lastParm = cons.GetParameters().Last();
                    if (lastParm.GetCustomAttribute<ParamArrayAttribute>() != null)
                    {
                        if (nParms == allParamTypes.Length + 1) // params has zero length
                            matchedCons = type.GetConstructor(allParamTypes.Append(lastParm.ParameterType).ToArray());
                        else if (allParamTypes.Skip(nParms - 1) 
                            .All(pt => lastParm.ParameterType.GetElementType().IsAssignableFrom(pt))) // final params can be coalesced into params array
                        {
                            matchedCons = type.GetConstructor(allParamTypes.Take(nParms - 1).Append(lastParm.ParameterType).ToArray());
                        }
                        if (matchedCons != null)
                            break;
                    }
                }
            }

            return matchedCons;
        }

        public static object[] ConformParameterValuesToParameters(this ParameterInfo[] parms, object[] values)
        {
            if (parms.Length == 0 || parms.Last().GetCustomAttribute<ParamArrayAttribute>() == null)
                return values;

            Array typedArray;
            Type elementType = parms.Last().ParameterType.GetElementType();
            // deal with final params array
            typedArray = Array.CreateInstance(elementType, values.Length - parms.Length + 1);
            var paramsArray = values.Skip(parms.Length - 1).ToArray();
            int idx = 0;
            foreach (object paramsVal in paramsArray)
                typedArray.SetValue(paramsVal, idx++);

            return values.Take(parms.Length - 1).Append(typedArray).ToArray();
        }
    }
}
