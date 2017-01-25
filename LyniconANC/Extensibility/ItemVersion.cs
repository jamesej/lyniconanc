using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Utility;
using Newtonsoft.Json;
using Lynicon.Repositories;
using Lynicon.Collation;
using Lynicon.Models;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// Item Version is a dictionary of versions with the version value for each.  The
    /// absence of a value indicates the type of the versioned item is specified not
    /// to have that kind of versioning.  A Null value indicates the that the type of the
    /// versioned item does allow versioning of this kind, but this item is shared across
    /// all versions.  Null values also allow a version to have an abstract form that implies
    /// a set of specific version, the set gained by all combinations of substituting valid
    /// specific version values for the null values.
    /// </summary>
    [Serializable]
    public class ItemVersion : Dictionary<string, object>, IEquatable<ItemVersion>, ISerializable
    {
        /// <summary>
        /// Get the current version (from the global VersionManager)
        /// </summary>
        public static ItemVersion Current
        {
            get
            {
                return VersionManager.Instance.CurrentVersion;
            }
        }

        /// <summary>
        /// Fix value types, needs to correct for JSON deserialization of ints to Int64
        /// </summary>
        /// <param name="o">Object to fix deserialization for</param>
        /// <returns>Object fixed for deserialization</returns>
        public static object CorrectValueType(object o)
        {
            if (o is Int64)
                return (object)Convert.ToInt32(o);
            else
                return o;
        }

        /// <summary>
        /// Create an empty ItemVersion
        /// </summary>
        public ItemVersion()
        { }
        /// <summary>
        /// Create an ItemVersion from dictionary (which could be another ItemVersion)
        /// </summary>
        /// <param name="dict">Dictionary of string keys to object values</param>
        public ItemVersion(IDictionary<string, object> dict) : base(dict)
        { }
        /// <summary>
        /// Special constructor for use in JSON deserialization which corrects integer values to Int32 from Int64
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public ItemVersion(SerializationInfo info, StreamingContext context)
        {
            ItemVersion newIv = JsonConvert.DeserializeObject<ItemVersion>((string)info.GetValue("vsn", typeof(string)));
            newIv.Do(kvp => this.Add(kvp.Key, kvp.Value is Int64 ? Convert.ToInt32(kvp.Value) : kvp.Value));
        }
        /// <summary>
        /// Creates the ItemVersion for a data object which can be a summary, a container or a content item
        /// </summary>
        /// <param name="o">Data object for which to create its ItemVersion</param>
        public ItemVersion(object o) : base()
        {
            if (o is Summary)
            {
                var vsn = ((Summary)o).Version;
                if (vsn != null)
                    vsn.Do(kvp => this.Add(kvp.Key, kvp.Value));
            } 
            else
            {
                object container = null;
                if (o is IContentContainer)
                    container = o;
                else
                    container = Collator.Instance.GetContainer(o);

                VersionManager.Instance.GetVersion(container)
                    .Do(kvp => this.Add(kvp.Key, kvp.Value));
            }

        }
        /// <summary>
        /// Create an ItemVersion from a serialized string
        /// </summary>
        /// <param name="desc">ItemVersion serialized to string</param>
        public ItemVersion(string desc)
            : base()
        {
            if (string.IsNullOrEmpty(desc))
                return;
            ItemVersion newIv = JsonConvert.DeserializeObject<ItemVersion>(desc);
            newIv.Do(kvp => this.Add(kvp.Key, kvp.Value is Int64 ? Convert.ToInt32(kvp.Value) : kvp.Value));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("vsn", this.ToString(), typeof(string));
        }

        /// <summary>
        /// With some versionings, the different versions are all accessible on different urls (e.g. different language versions).
        /// These are 'addressable'.  Other versionings only have one version which is accessible (e.g. published/unpublished).  These
        /// are 'unaddressable'.  This returns the version key/values in this version which are addressable.
        /// </summary>
        /// <returns>Version with addressable key/values only</returns>
        public ItemVersion GetAddressablePart()
        {
            var addressablePart = new ItemVersion();
            this.Where(kvp => VersionManager.Instance.AddressableVersionKeys.Contains(kvp.Key))
                .Do(kvp => addressablePart.Add(kvp.Key, kvp.Value));
            return addressablePart;
        }

        /// <summary>
        /// With some versionings, the different versions are all accessible on different urls (e.g. different language versions).
        /// These are 'addressable'.  Other versionings only have one version which is accessible (e.g. published/unpublished).  These
        /// are 'unaddressable'.  This returns the version key/values in this version which are unaddressable.
        /// </summary>
        /// <returns>Version with unaddressable key/values only</returns>
        public ItemVersion GetUnaddressablePart()
        {
            var unaddressablePart = new ItemVersion();
            this.Where(kvp => VersionManager.Instance.UnaddressableVersionKeys.Contains(kvp.Key))
                .Do(kvp => unaddressablePart.Add(kvp.Key, kvp.Value));
            return unaddressablePart;
        }

        /// <summary>
        /// Extracts only the key/values in this version which are used to version items of type t
        /// </summary>
        /// <param name="t">Content type of an item</param>
        /// <returns>Key/values used to version type t</returns>
        public ItemVersion GetApplicablePart(Type t)
        {
            return VersionManager.Instance.GetApplicableVersion(this, t);
        }

        /// <summary>
        /// Take the key/values of the argument ItemVersion and return the result of copying them all
        /// into the current ItemVersion, overwriting any existing ones
        /// </summary>
        /// <param name="other">An item version</param>
        /// <returns>A new ItemVersion made by copying the keys of the argument over the keys of this one</returns>
        public ItemVersion Superimpose(ItemVersion other)
        {
            var combined = new ItemVersion(this);
            other.Do(kvp => combined[kvp.Key] = kvp.Value);
            return combined;
        }

        public ItemVersion Overlay(ItemVersion other)
        {
            var overlaid = new ItemVersion(this);
            other.Do(kvp =>
                {
                    if (overlaid.ContainsKey(kvp.Key))
                        overlaid[kvp.Key] = kvp.Value;
                });
            return overlaid;
        }

        /// <summary>
        /// Mask returns a new ItemVersion made by taking all the key value pairs from the argument ItemVersion
        /// and where the value is null, and this ItemVersion has the corresponding key, taking the value from
        /// this ItemVersion.  The null values in the argument can be viewed as holes in the mask.
        /// </summary>
        /// <param name="other">ItemVersion with which to mask this one to get the result</param>
        /// <returns>Resulting ItemVersion</returns>
        public ItemVersion Mask(ItemVersion other)
        {
            var masked = new ItemVersion();
            other.Do(kvp =>
                {
                    if (kvp.Value == null && this.ContainsKey(kvp.Key))
                        masked.Add(kvp.Key, this[kvp.Key]);
                    else if (kvp.Value != null)
                        masked.Add(kvp.Key, kvp.Value);
                });
            return masked;
        }

        /// <summary>
        /// Find the most specific ItemVersion which contains both this ItemVersion and the argument
        /// </summary>
        /// <param name="other">Other ItemVersion</param>
        /// <returns>Most specific ItemVersion containing this and the other</returns>
        public ItemVersion LeastAbstractCommonVersion(ItemVersion other)
        {
            var extended = new ItemVersion();
            other.Do(kvp =>
                {
                    if (this.ContainsKey(kvp.Key) && this[kvp.Key] == kvp.Value)
                        extended.Add(kvp.Key, kvp.Value);
                });
            return extended;
        }

        /// <summary>
        /// Test that this ItemVersion is in the set of specific ItemVersions covered by the abstract ItemVersion given
        /// as an argument.  In other words, if the argument has a specific key value, this ItemVersion has the same
        /// key with the same value.  Null-valued keys in the argument are ignored.
        /// </summary>
        /// <param name="other">An abstract ItemVersion to compare this one against</param>
        /// <returns>Whether this ItemVersion is contained by the argument</returns>
        public bool ContainedBy(ItemVersion other)
        {
            foreach (var key in this.Keys)
                if (other.ContainsKey(key) && other[key] != null && !other[key].Equals(this[key]))
                    return false;

            return true;
        }

        /// <summary>
        /// There is a specific ItemVersion which is contained by both this ItemVersion and the argument
        /// </summary>
        /// <param name="other">The other ItemVersion</param>
        /// <returns>Whether an common specific ItemVersion exists</returns>
        public bool Overlaps(ItemVersion other)
        {
            foreach (var key in this.Keys)
                if (other.ContainsKey(key) && other[key] != null && this[key] != null && !other[key].Equals(this[key]))
                    return false;

            return true;
        }

        /// <summary>
        /// Remove all the keys listed from this ItemVersion
        /// </summary>
        /// <param name="keys">List of keys to remove</param>
        public void RemoveKeys(IEnumerable<string> keys)
        {
            foreach (string key in keys)
                if (this.ContainsKey(key))
                    this.Remove(key);
        }

        /// <summary>
        /// Find all specific ItemVersions which are applicable to a type
        /// </summary>
        /// <param name="t">The type</param>
        /// <returns>All applicable ItemVersion</returns>
        public List<ItemVersion> MatchingVersions(Type t)
        {
            var vsn = new ItemVersion(this);
            var other = VersionManager.Instance.VersionForType(t);
            foreach (var key in this.Keys)
                if (!other.ContainsKey(key))
                    vsn.Remove(key);
            foreach (var key in other.Keys)
                if (!vsn.ContainsKey(key))
                    vsn.Add(key, null);
            return vsn.Expand();
        }

        /// <summary>
        /// Set the version of a container to this ItemVersion
        /// </summary>
        /// <param name="container">The container on which to set the ItemVersion</param>
        public void SetOnItem(object container)
        {
            VersionManager.Instance.SetVersion(this, container);
        }

        /// <summary>
        /// If this is an abstract ItemVersion, find all specific ItemVersions it contains.  If this
        /// is a specific ItemVersion, just return itself.
        /// </summary>
        /// <returns>List of contained specific ItemVersions</returns>
        public List<ItemVersion> Expand()
        {
            var vs = new List<ItemVersion> { new ItemVersion(this) };
            foreach (string key in this.Keys)
            {
                if (this[key] != null)
                    continue;

                var vals = VersionManager.Instance.VersionLists[key];
                if (vals == null || vals.Length == 0)
                    throw new Exception("Can't get current versions because current version has null value for unlistable key " + key);
                vs.Do(v => v[key] = vals[0]);
                var addVs = new List<ItemVersion>();
                vals.Skip(1).Do(val =>
                    vs.Do(v =>
                    {
                        var addV = new ItemVersion(v);
                        addV[key] = val;
                        addVs.Add(addV);
                    }));
                vs.AddRange(addVs);
            }

            return vs;
        }

        /// <summary>
        /// Return this ItemVersion as a serialized string
        /// </summary>
        /// <returns>Serialized string</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ItemVersion);
        }

        public override int GetHashCode()
        {
            // Note the hash is independent of the order of the keys in the dictionary
            unchecked
            {
                int hash = 0;
                foreach (var kvp in this)
                {
                    if (kvp.Value == null) // null-valued keys are not significant
                        continue;

                    hash += 31 * (kvp.Key == null ? 0 : kvp.Key.GetHashCode()) + (kvp.Value == null ? 0 : kvp.Value.GetHashCode());
                }
                return hash;
            }
        }

        public static bool operator ==(ItemVersion lhs, ItemVersion rhs)
        { 
            if (Object.ReferenceEquals(lhs, null))
            {
                if (Object.ReferenceEquals(rhs, null)) 
                    return true;
 
                return false;
            } 
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ItemVersion lhs, ItemVersion rhs)
        {
            return !(lhs == rhs);
        }

        #region IEquatable<ItemVersion> Members

        public bool Equals(ItemVersion other)
        {
            if (other == null || this.Values.Where(v => v != null).Count() != other.Values.Where(v => v != null).Count())
                return false;

            foreach (var kvp in this)
            {
                if (kvp.Value == null)
                    continue;

                if (!other.ContainsKey(kvp.Key) || other[kvp.Key] == null || !other[kvp.Key].Equals(kvp.Value))
                    return false;
            }
            return true;
        }

        #endregion

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            foreach (string key in this.Keys.ToList())
            {
                if (this[key] != null && this[key].GetType() == typeof(Int64))
                    this[key] = (int)(Int64)this[key];
            }
        }
    }
}
