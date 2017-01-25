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

namespace Lynicon.Collation
{
    /// <summary>
    /// This contains an Address together with an ItemVersion, thus specifying a particular version of a content item
    /// </summary>
    [Serializable]
    public class VersionedAddress : Address, IEquatable<VersionedAddress>
    {
        /// <summary>
        /// Creates a VersionedAddress from an address using the current ItemVersion
        /// </summary>
        /// <param name="a">The Address</param>
        /// <returns>The resulting VersionedAddress</returns>
        public static VersionedAddress CurrentFromAddress(Address a)
        {
            ItemVersion curr = VersionManager.Instance.CurrentVersionForType(a.Type);
            return new VersionedAddress(a, curr);
        }

        /// <summary>
        /// Creates a list of VersionedAddresses by taking the (possibly abstract) version of the versioned
        /// address of a container, and producing all the specific versions contained in it attached to the
        /// ItemId of the container
        /// </summary>
        /// <param name="container">The container</param>
        /// <returns>List of specific VersionedAddresses</returns>
        public static List<VersionedAddress> CreateExpanded(object container)
        {
            var vsn = new ItemVersion(container);
            Address a = new Address(container);
            var res = new List<VersionedAddress>();
            foreach (var v in vsn.MatchingVersions(a.Type))
                res.Add(new VersionedAddress(a, v));

            return res;
        }

        ItemVersion version = null;
        /// <summary>
        /// The version of the VersionedAddress
        /// </summary>
        public ItemVersion Version
        {
            get
            {
                if (version == null && Type != null)
                    return VersionManager.Instance.CurrentVersion;
                else
                    return version;
            }
            set
            {
                version = value;
            }
        }

        /// <summary>
        /// The Address of the VersionedAddress
        /// </summary>
        public Address Address
        {
            get
            {
                return new Address(this.Type, this);
            }
        }

        /// <summary>
        /// Create an empty VersionedAddress
        /// </summary>
        public VersionedAddress() { }
        /// <summary>
        /// Create the VersionedAddress of a container
        /// </summary>
        /// <param name="container">The container</param>
        public VersionedAddress(object container) :
            base(container)
        {
            Version = new ItemVersion(container);
        }
        /// <summary>
        /// Create a VersionedAddress from its serialized string
        /// </summary>
        /// <param name="desc">The serialized string</param>
        public VersionedAddress(string desc) : base(desc == null ? null : desc.UpTo(" "))
        {
            if (desc == null) return;
            Version = new ItemVersion(desc.After(" "));
        }
        /// <summary>
        /// Create (copy) a VersionedAddress from another VersionedAddress
        /// </summary>
        /// <param name="va">The other VersionedAddress</param>
        public VersionedAddress(VersionedAddress va) : this(va, va.Version)
        { }
        /// <summary>
        /// Create a VersionedAddress from an Address and the current ItemVersion
        /// </summary>
        /// <param name="a">The address</param>
        public VersionedAddress(Address a) : this(a, VersionManager.Instance.CurrentVersion)
        { }
        /// <summary>
        /// Create a VersionedAddress from an Address and an ItemVersion
        /// </summary>
        /// <param name="a">The Address</param>
        /// <param name="version">The ItemVersion</param>
        public VersionedAddress(Address a, ItemVersion version) :
            base(a.Type, a.GetAsContentPath())
        {
            Version = VersionManager.Instance.GetApplicableVersion(version, a.Type);
        }
        /// <summary>
        /// Create a VersionedAddress from a Type, an address path and an ItemVersion
        /// </summary>
        /// <param name="t">the Type</param>
        /// <param name="path">the address path</param>
        /// <param name="version">the ItemVersion</param>
        public VersionedAddress(Type t, string path, ItemVersion version) :
            this(new Address(t, path), version)
        { }

        /// <summary>
        /// Create an ItemVersionedId from this VersionedAddress, masking the ItemVersion with the supplied ItemVersion
        /// </summary>
        /// <param name="mask">The ItemVersion to mask with</param>
        /// <returns>The resulting ItemVersionedId</returns>
        public ItemVersionedId Mask(ItemVersion mask)
        {
            var ivid = new ItemVersionedId(this);
            ivid.Version = ivid.Version.Mask(mask);
            return ivid;
        }

        /// <summary>
        /// Serialize a VersionedAddress
        /// </summary>
        /// <returns>Serialized VersionedAddress</returns>
        public override string ToString()
        {
            if (Version == null)
                return "null";
            return base.ToString() + " " + Version.ToString();
        }

        #region equality

        public override bool Equals(object obj)
        {
            return this.Equals(obj as VersionedAddress);
        }

        public bool Equals(VersionedAddress va)
        {
            if (Object.ReferenceEquals(va, null))
                return false;

            if (Object.ReferenceEquals(this, va))
                return true;

            if (this.GetType() != va.GetType())
                return false;

            bool eq = (Version == va.Version) && base.Equals(va);

            return eq;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 0;
                hash = 31 * hash + base.GetHashCode();
                hash = 31 * hash + (Version == null ? 0 : Version.GetHashCode());

                return hash;
            }
        }

        public static bool operator ==(VersionedAddress lhs, VersionedAddress rhs)
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

        public static bool operator !=(VersionedAddress lhs, VersionedAddress rhs)
        {
            return !(lhs == rhs);
        }

        #endregion
    }
}
