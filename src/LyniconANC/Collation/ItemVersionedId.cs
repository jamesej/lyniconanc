using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Repositories;
using Lynicon.Utility;
using Newtonsoft.Json;
using Lynicon.Services;

namespace Lynicon.Collation
{
    /// <summary>
    /// Uniquely identifies a specific version of a content item
    /// </summary>
    [JsonConverter(typeof(LyniconIdentifierTypeConverter))]
    public class ItemVersionedId : ItemId, IEquatable<ItemVersionedId>
    {
        /// <summary>
        /// Creates an ItemVersionedId for a content item specified by its ItemId and an
        /// ItemVersion without removing any version keys inapplicable to the type of the item
        /// </summary>
        /// <param name="id">The ItemId of the content item</param>
        /// <param name="vers">The version required</param>
        /// <returns>The ItemVersionedId made of the given ItemId and ItemVersion</returns>
        public static ItemVersionedId CreateWithoutVersionApplicability(ItemId id, ItemVersion vers)
        {
            return new ItemVersionedId { Id = id, Version = vers };
        }

        /// <summary>
        /// Given a container, extract its ItemVersionedId (which may be abstract) and generate
        /// a list of all the specifically versioned ItemVersionedIds that are included in it
        /// </summary>
        /// <param name="container">A container</param>
        /// <returns>A list of specific ItemVersionedIds</returns>
        public static List<ItemVersionedId> CreateExpanded(LyniconSystem sys, object container)
        {
            var vsn = new ItemVersion(sys, container);
            ItemId iid = new ItemId(sys.Collator, container);
            var res = new List<ItemVersionedId>();
            foreach (var v in vsn.MatchingVersions(sys.Versions, iid.Type))
                res.Add(new ItemVersionedId(iid, v));

            return res;
        }

        public static explicit operator ItemVersionedId(string s)
        {
            return new ItemVersionedId(s);
        }

        ItemVersion version = null;
        /// <summary>
        /// The Version of the ItemVersionedId
        /// </summary>
        public ItemVersion Version
        {
            get
            {
                return version;
            }
            protected set
            {
                version = value;
            }
        }

        /// <summary>
        /// Create an empty ItemVersionedId
        /// </summary>
        public ItemVersionedId() { }
        /// <summary>
        /// Create the ItemVersionedId of a given container or content item
        /// </summary>
        /// <param name="container">The container or content item</param>
        public ItemVersionedId(LyniconSystem sys, object o) :
            base(sys.Collator, o)
        {
            Version = new ItemVersion(sys, o);
        }
        /// <summary>
        /// Create the ItemVersionedId from a serialized string
        /// </summary>
        /// <param name="desc">The serialized string</param>
        public ItemVersionedId(string desc) : base(desc == null ? null : desc.UpTo(" "))
        {
            if (desc == null) return;
            Version = new ItemVersion(desc.After(" "));
        }
        /// <summary>
        /// Create (copy) an ItemVersionedId from another ItemVersionedId
        /// </summary>
        /// <param name="ivid">The other ItemVersionedId</param>
        public ItemVersionedId(ItemVersionedId ivid) : this(ivid, ivid.Version)
        { }
        /// <summary>
        /// Create an ItemVersionedId from an ItemId
        /// </summary>
        /// <param name="id">The ItemId</param>
        public ItemVersionedId(ItemId id) : this(id, null)
        { }
        /// <summary>
        /// Create an ItemVersionedId from an ItemId and an ItemVersion
        /// </summary>
        /// <param name="id">the ItemId</param>
        /// <param name="version">the ItemVersion</param>
        public ItemVersionedId(ItemId id, ItemVersion version) :
            base(id.Type, id.Id)
        {
            Version = version;
        }
        /// <summary>
        /// Create an ItemVersionedId from a type, an id and an ItemVersion
        /// </summary>
        /// <param name="type">the type</param>
        /// <param name="id">the id</param>
        /// <param name="version">the ItemVersion</param>
        public ItemVersionedId(Type type, object id, ItemVersion version):
            base(type, id)
        {
            Version = version ?? throw new ArgumentException("ItemVersion cannot be null");
        }

        /// <summary>
        /// Mask (see ItemVersion for definition) the version of this ItemVersionedId
        /// with a given version, and return a copy with the resulting version.
        /// </summary>
        /// <param name="mask">The ItemVersion to use as a mask</param>
        /// <returns>A copy of this ItemVersionedId with its ItemVersion masked</returns>
        public ItemVersionedId Mask(ItemVersion mask)
        {
            var ivid = new ItemVersionedId(this);
            ivid.Version = ivid.Version.Mask(mask);
            return ivid;
        }

        /// <summary>
        /// Serialize the ItemVersionedId
        /// </summary>
        /// <returns>Output serialization</returns>
        public override string ToString()
        {
            if (Version == null)
                return "null";
            return base.ToString() + " " + Version.ToString();
        }

        #region equality

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ItemVersionedId);
        }

        public bool Equals(ItemVersionedId ivi)
        {
            if (Object.ReferenceEquals(ivi, null))
                return false;

            if (Object.ReferenceEquals(this, ivi))
                return true;

            if (this.GetType() != ivi.GetType())
                return false;

            bool eq = Id != null && (Id.Equals(ivi.Id)) && (Type == ivi.Type) && (Version == ivi.Version);

            return eq;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 0;
                hash = 31 * hash + (Id == null ? 0 : Id.GetHashCode());
                hash = 31 * hash + (Type == null ? 0 : Type.GetHashCode());
                hash = 31 * hash + (Version == null ? 0 : Version.GetHashCode());

                return hash;
            }
        }

        public static bool operator ==(ItemVersionedId lhs, ItemVersionedId rhs)
        {
            // Check for null on left side. 
            if (Object.ReferenceEquals(lhs, null))
            {
                if (Object.ReferenceEquals(rhs, null))
                {
                    // null == null = true. 
                    return true;
                }

                // Only the left side is null. 
                return false;
            }
            // Equals handles case of null on right side. 
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ItemVersionedId lhs, ItemVersionedId rhs)
        {
            return !(lhs == rhs);
        }

        #endregion
    }
}
