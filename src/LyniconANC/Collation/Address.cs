using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Lynicon.Attributes;
using Lynicon.Utility;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Lynicon.Repositories;
using Lynicon.Models;
using Microsoft.AspNetCore.Routing;
using System.Net;
using Newtonsoft.Json;
using Lynicon.Extensibility;
using System.Collections.ObjectModel;

namespace Lynicon.Collation
{
    /// <summary>
    /// Information extracted from a url which maps to one and only one content item
    /// </summary>
    [JsonConverter(typeof(LyniconIdentifierTypeConverter))]
    [TypeConverter(typeof(AddressTypeConverter))]
    public class Address : ReadOnlyDictionary<string, object>, IEquatable<Address>
    {
        public static explicit operator Address(string s)
        {
            return new Address(s);
        }

        private static Dictionary<string, object> FromPath(string path)
        {
            var dict = new Dictionary<string, object>();
            var pathParts = (path ?? "").Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < pathParts.Length; i++)
                dict.Add("_" + i.ToString(), pathParts[i].ToLower());
            return dict;
        }

        private static Dictionary<string, object> FromSerialized(string serialized)
        {
            var dict = new Dictionary<string, object>();
            foreach (string kvpStr in serialized.After(":").Split('&'))
            {
                dict.Add(WebUtility.UrlDecode(kvpStr.UpTo("=")),
                    WebUtility.UrlDecode(kvpStr.After("=")));
            }
            return dict;
        }

        private static Dictionary<string, object> FromContainer(object cont)
        {

            var addressProps = cont.GetType().GetProperties()
                .Select(pi => new
                {
                    pi,
                    attr = pi.GetCustomAttribute<AddressComponentAttribute>(),
                    preference = (pi.GetCustomAttribute<ScaffoldColumnAttribute>()?.Scaffold ?? true) ? 1 : 0 // prefer attributes that can be edited by the user: these should override others
                })
                .Where(pii => pii.attr != null)
                .OrderByDescending(pii => pii.preference)
                .ToList();
            if (addressProps.Any(ap => ap.attr.UsePath))
                return FromPath((string)addressProps.First(ap => ap.attr.UsePath).pi.GetValue(cont));
            else
            {
                var dict = new Dictionary<string, object>();
                addressProps.Do(pii =>
                {
                    string key = pii.attr.RouteKey ?? ("_" + pii.pi.Name);
                    if (!dict.ContainsKey(key))
                        dict.Add(key, pii.pi.GetValue(cont));
                });

                if (dict.Count == 0)
                {
                    var keyPi = cont.GetType().GetProperties()
                        .FirstOrDefault(pi => pi.GetCustomAttribute<KeyAttribute>() != null);
                    if (keyPi != null)
                        dict.Add("_id", keyPi.GetValue(cont).ToString());
                }
                return dict;
            }
        }

        List<string> matched = new List<string>();
        Dictionary<string, string> conversionFormats = new Dictionary<string, string>();
        Type conversionType = null;

        /// <summary>
        /// Returns the C# string formats specified for each address element
        /// </summary>
        public Dictionary<string, string> ConversionFormats
        {
            get
            {
                if (this.Type != null && conversionType != this.Type)
                {
                    conversionFormats = this.Type.GetProperties()
                        .Select(pi => pi.GetCustomAttribute<AddressComponentAttribute>())
                        .Where(aca => aca != null && aca.ConversionFormat != null)
                        .ToDictionary(aca => aca.RouteKey, aca => aca.ConversionFormat);
                    conversionType = this.Type;
                }
                return conversionFormats;
            }
        }

        private Type type = null;
        /// <summary>
        /// The type of the content item addressed
        /// </summary>
        public Type Type
        {
            get
            {
                return type;
            }
            protected set
            {
                Type unexType = value.UnextendedType();
                if (!ContentTypeHierarchy.AllContentTypes.Contains(unexType))
                    throw new ArgumentException("Can't set address type to non-content type " + value.FullName + ". You may want to register this type using UseTypeSetup().");
                type = unexType;
            }
        }

        /// <summary>
        /// Constructs an address from any string-object dictionary
        /// </summary>
        /// <param name="dict">a string-object dictionary</param>
        public Address(Type type, IDictionary<string, object> dict) : base(dict)
        {
            this.Type = type;
        }
        /// <summary>
        /// Constructs an address from the type of the content item and a content 'path', a string made up of
        /// a series of string indexes separated by ampersands
        /// </summary>
        /// <param name="type">Type of the addressed item</param>
        /// <param name="contentPath">Series of string indexes separated by ampersands</param>
        public Address(Type type, string contentPath) : base(FromPath(contentPath))
        {
            this.Type = type;
        }
        /// <summary>
        /// Constructs an address from the type of a content item and the route data of the request for the item
        /// </summary>
        /// <param name="type">Type of the content item</param>
        /// <param name="rd">Route data of request for item</param>
        public Address(Type type, RouteData rd) : this(type, Collator.Instance.GetAddress(type, rd))
        { }
        /// <summary>
        /// Constructs an address from a serialized string form (created via .ToString())
        /// </summary>
        /// <param name="serialized">the serialized string</param>
        public Address(string serialized) : base(FromSerialized(serialized))
        {
            this.Type = ContentTypeHierarchy.GetContentType(serialized.UpTo(":"));
        }

        /// <summary>
        /// Constructs an address by extracting it from a container or a content item where AddressComponent attributes were
        /// used to map content fields to the address
        /// </summary>
        /// <param name="cont">The container or content item</param>
        public Address(object cont)
            : base(FromContainer(cont))
        {
            this.Type = Collator.GetContentType(cont);
        }

        /// <summary>
        /// Gets the value of a given key in the address as a string
        /// </summary>
        /// <param name="key">The key to get the value for</param>
        /// <returns></returns>
        public virtual string GetAsString(string key)
        {
            if (ConversionFormats.ContainsKey(key))
                return string.Format("{0:" + ConversionFormats[key] + "}", this[key]);
            else
                return (this[key] ?? "").ToString();
        }

        /// <summary>
        /// Converts the address into a content path (the values in key order separated by '&'s)
        /// </summary>
        /// <returns>Content path</returns>
        public string GetAsContentPath()
        {
            return this.OrderBy(kvp => kvp.Key).Select(kvp => GetAsString(kvp.Key)).Join("&"); //.ToLower();
        }

        /// <summary>
        /// Get a query body which converts the address into a query which should return the item from that address
        /// when used as an argument to a Repository method. Use the primary data system.
        /// </summary>
        /// <typeparam name="T">Container type query is applied to, which the address addresses</typeparam>
        /// <returns>The query body</returns>
        public Func<IQueryable<T>, IQueryable<T>> GetAsQueryBody<T>()
        {
            return GetAsQueryBody<T>(Collator.Instance);
        }
        /// <summary>
        /// Get a query body which converts the address into a query which should return the item from that address
        /// when used as an argument to a Repository method.
        /// </summary>
        /// <typeparam name="T">Container type query is applied to, which the address addresses</typeparam>
        /// <param name="coll">The collator of the data system in which the query body will run</param>
        /// <returns>The query body</returns>
        public Func<IQueryable<T>, IQueryable<T>> GetAsQueryBody<T>(Collator coll)
        {
            Func<IQueryable<T>, IQueryable<T>> queryBody = null;

            if (this.ContainsKey("_id"))
            {
                var idProp = coll.GetIdProperty(typeof(T));
                return iq => iq.Where(LinqX.GetPropertyTest<T>(idProp.Name, this["_id"]));
            }

            Dictionary<string, string> keyProps = typeof(T).GetProperties()
                .Select(pi => new { pi, attr = pi.GetCustomAttribute<AddressComponentAttribute>() })
                .Where(pii => pii.attr != null)
                .ToDictionary(pii => pii.attr.UsePath ? "{Path}" : (pii.attr.RouteKey ?? ("_" + pii.pi.Name)),
                    pii => pii.pi.Name);

            if (keyProps.ContainsKey("{Path}"))
            {
                queryBody = iq => iq.Where(LinqX.GetPropertyTest<T>(keyProps["{Path}"], GetAsContentPath()));
            }
            else
            {
                foreach (string key in keyProps.Keys)
                {
                    string propName = keyProps[key];
                    object matchVal;
                    if (this.ContainsKey(key))
                        matchVal = this[key];
                    else
                    {
                        // this address doesn't have a value for this address component property so use a default value
                        Type keyType = typeof(T).GetProperty(propName).PropertyType;
                        if (keyType.IsValueType())
                            matchVal = Activator.CreateInstance(keyType);
                        else
                            matchVal = null;
                    }
                    if (queryBody == null)
                        queryBody = iq => iq.Where(LinqX.GetPropertyTest<T>(propName, matchVal));
                    else
                    {
                        var innerQueryBody = queryBody;
                        queryBody = iq => innerQueryBody(iq).Where(LinqX.GetPropertyTest<T>(propName, matchVal));
                    }
                }
                if (queryBody == null)
                    queryBody = iq => iq;
            }

            return queryBody;
        }

        /// <summary>
        /// Sets any fields on a content item which map to an address to have the values in this address
        /// </summary>
        /// <param name="item">Content item on which to set address fields</param>
        public void SetAddressFields(object item)
        {
            var propMap = item.GetType()
                .GetProperties()
                .Select(pi => new { Prop = pi, Attr = pi.GetCustomAttribute<AddressComponentAttribute>() })
                .Where(pii => pii.Attr != null)
                .ToLookup(pii => pii.Attr.RouteKey ?? "|PATH|", pii => pii.Prop);

            foreach (var kvp in this)
            {
                List<PropertyInfo> pis = new List<PropertyInfo>();
                if (propMap.Contains(kvp.Key))
                    pis = propMap[kvp.Key].ToList();
                else
                    pis = new List<PropertyInfo> { item.GetType().GetProperty(kvp.Key) };

                if (pis.Count == 0) continue;

                object val = kvp.Value;

                foreach (var pi in pis)
                {
                    if (pi == null)
                        continue;

                    if (val is string && pi.PropertyType != typeof(string))
                    {
                        TypeConverter typeConverter = TypeDescriptor.GetConverter(pi.PropertyType);
                        val = typeConverter.ConvertFromString(val as string);
                    }

                    pi.SetValue(item, val);
                }
            }

            // Clear properties on item with no matching entry in the Address to default values
            foreach (var key in propMap.Select(il => il.Key).Except(this.Keys))
            {
                if (key == "|PATH|")
                {
                    foreach (var pi in propMap["|PATH|"])
                        pi.SetValue(item, this.GetAsContentPath());
                }
                else
                {
                    foreach (var pi in propMap[key])
                    {
                        Type propType = pi.PropertyType;
                        if (propType.IsValueType())
                            pi.SetValue(item, Activator.CreateInstance(propType));
                        else
                            pi.SetValue(item, null);
                    }
                }
            }
        }

        /// <summary>
        /// Set a key as having been matched in an algorithm which needs to match all
        /// the keys of the Address
        /// </summary>
        /// <param name="key">The address element key</param>
        public virtual void SetMatched(string key)
        {
            matched.Add(key);
        }

        /// <summary>
        /// Whether all the address element keys have been matched
        /// </summary>
        public bool FullyMatched
        {
            get
            {
                if (matched.Contains("_id")) return true;
                if (Keys.Where(k => k != "_id").All(k => matched.Contains(k))) return true;
                return false;
            }
        }

        public Address FixCase()
        {
            var dict = new Dictionary<string, object>();
            foreach (var key in this.Keys.ToList())
            {
                if (this[key] is string)
                    dict[key] = ((string)this[key]).ToLower();
                else
                    dict[key] = this[key];
            }
            
            return new Address(this.Type, dict);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Address);
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public static bool operator ==(Address lhs, Address rhs)
        {
            if (Object.ReferenceEquals(lhs, null))
            {
                if (Object.ReferenceEquals(rhs, null))
                    return true;

                return false;
            }
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Address lhs, Address rhs)
        {
            return !(lhs == rhs);
        }

        #region IEquatable<Address> Members

        public bool Equals(Address other)
        {
            if (other == null || this.Type != other.Type || this.Count != other.Count)
                return false;

            return this.ToString().Equals(other.ToString());
        }

        #endregion

        /// <summary>
        /// Serialize the address.  Equality of serialized string is equivalent to equality of the address.
        /// </summary>
        /// <returns>Address serialized to string</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(this.Type.FullName + ":");
            bool first = true;
            foreach (var kvp in this.OrderBy(kvp => kvp.Key))
            {
                if (first)
                    first = false;
                else
                    sb.Append("&");
                sb.Append(WebUtility.UrlEncode(kvp.Key));
                sb.Append("=");
                string val = (kvp.Value ?? "").ToString();
                //if (kvp.Value is string)
                //    val = val.ToLower();
                sb.Append(WebUtility.UrlEncode(val));
            }
            return sb.ToString();
        }
    }
}
