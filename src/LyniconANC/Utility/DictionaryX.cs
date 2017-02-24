using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Utility
{
    /// <summary>
    /// Some extension methods for dictionaries
    /// </summary>
    public static class DictionaryX
    {
        /// <summary>
        /// Get a value from a string x object dictionary, casting the object to a type
        /// </summary>
        /// <typeparam name="T">Type to cast to</typeparam>
        /// <param name="dict">the string x object dictionary</param>
        /// <param name="name">the key of the dictionary</param>
        /// <returns>the value of the key cast to T</returns>
        public static T Get<T>(this Dictionary<string, object> dict, string name)
        {
            if (!dict.ContainsKey(name))
                return default(T);
            return (T)dict[name];
        }
        /// <summary>
        /// Get a value from a string x object dictionary, casting the object to a type, with a default if the key does not exist
        /// </summary>
        /// <typeparam name="T">Type to cast to</typeparam>
        /// <param name="dict">the string x object dictionary</param>
        /// <param name="name">the key of the dictionary</param>
        /// <param name="def">the default value</param>
        /// <returns>the value of the key cast to T</returns>
        public static T Get<T>(this Dictionary<string, object> dict, string name, T def)
        {
            if (!dict.ContainsKey(name))
                return def;
            return (T)dict[name];
        }

        /// <summary>
        /// Get a value from a string x object dictionary, returning the default value of the type if the key does not
        /// exist or the value is null, and providing a selector function which otherwise will convert the value to a given type
        /// </summary>
        /// <typeparam name="TEntry">Underlying type of the dictionary value</typeparam>
        /// <typeparam name="T">Type to which the value is converted</typeparam>
        /// <param name="dict">the string x object dictionary</param>
        /// <param name="name">the key of the dictionary</param>
        /// <param name="selector">a function to convert a TEntry to a T</param>
        /// <returns>converted dictionary value, or default(T) if there is none</returns>
        public static T GetSelectOrDefault<TEntry, T>(this Dictionary<string, object> dict, string name, Func<TEntry, T> selector ) where TEntry : class
        {
            if (!dict.ContainsKey(name))
                return default(T);
            TEntry entry = dict[name] as TEntry;
            if (entry == null)
                return default(T);
            return selector(entry);
        }
        /// <summary>
        /// Get a value from a object x object dictionary, returning the default value of the type if the key does not
        /// exist or the value is null, and providing a selector function which otherwise will convert the value to a given type
        /// </summary>
        /// <typeparam name="TEntry">Underlying type of the dictionary value</typeparam>
        /// <typeparam name="T">Type to which the value is converted</typeparam>
        /// <param name="dict">the object x object dictionary</param>
        /// <param name="key">the key of the dictionary</param>
        /// <param name="selector">a function to convert a TEntry to a T</param>
        /// <returns>converted dictionary value, or default(T) if there is none</returns>
        public static T GetSelectOrDefault<TEntry, T>(this IReadOnlyDictionary<object, object> dict, object key, Func<TEntry, T> selector) where TEntry : class
        {
            if (!dict.ContainsKey(key))
                return default(T);
            TEntry entry = dict[key] as TEntry;
            if (entry == null)
                return default(T);
            return selector(entry);
        }

        /// <summary>
        /// Add all the keys of another string x object dictionary to this one where they do not already exist in this dictionary
        /// </summary>
        /// <param name="dict">the string x object dictionary</param>
        /// <param name="otherDict">another string x object dictionary</param>
        /// <returns>this string x object dictionary after keys from the other have been added</returns>
        public static Dictionary<string, object> Merge(this Dictionary<string, object> dict, Dictionary<string, object> otherDict)
        {
            if (otherDict == null)
                return dict;
            foreach (var kvp in otherDict)
                if (!dict.ContainsKey(kvp.Key))
                    dict.Add(kvp.Key, kvp.Value);

            return dict;
        }
        /// <summary>
        /// Add all the keys of another object x object dictionary to this one where they do not already exist in this dictionary
        /// </summary>
        /// <param name="dict">the object x object dictionary</param>
        /// <param name="otherDict">another string x object dictionary</param>
        /// <returns>this string x object dictionary after keys from the other have been added</returns>
        public static IReadOnlyDictionary<object, object> Merge(this IReadOnlyDictionary<object, object> dict, IDictionary<string, object> otherDict)
        {
            if (otherDict == null)
                return dict;
            var newDict = dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            foreach (var kvp in otherDict)
                if (!newDict.ContainsKey(kvp.Key))
                    newDict.Add(kvp.Key, kvp.Value);

            return new ReadOnlyDictionary<object, object>(newDict);
        }
    }
}
