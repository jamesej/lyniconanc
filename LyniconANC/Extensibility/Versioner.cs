using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Collation;
using Lynicon.Membership;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// Base abstract class for the rules and methods which operate a versioning system
    /// </summary>
    public abstract class Versioner
    {
        /// <summary>
        /// Function to return whether this versioning system should be applied to a given type
        /// </summary>
        protected Func<Type, bool> IsVersionable { get; set; }

        /// <summary>
        /// The version key used in an ItemVersion for this versioning system
        /// </summary>
        public abstract string VersionKey { get; }

        /// <summary>
        /// List of all possible version values
        /// </summary>
        public abstract object[] AllVersionValues { get; }

        /// <summary>
        /// The version value which is shown to a public user (null if all version values are shown)
        /// </summary>
        public abstract object PublicVersionValue { get; }

        /// <summary>
        /// Whether the different versions are all available on different urls (or else only
        /// one version is available at once on one url)
        /// </summary>
        public abstract bool IsAddressable { get; }

        /// <summary>
        /// Create a versioner
        /// </summary>
        public Versioner()
        {
            this.IsVersionable = null;
        }

        /// <summary>
        /// Create a versioner supplying the test for whether a content type has the versioning system applied to it
        /// </summary>
        /// <param name="versionKey">The version key for the versioner</param>
        /// <param name="isVersionable">function testing whether a content type is versionable</param>
        public Versioner(Func<Type, bool> isVersionable) : this()
        {
            this.IsVersionable = isVersionable;
        }

        /// <summary>
        /// Whether a container type is can be versioned in this system
        /// </summary>
        /// <param name="containerType">The container type</param>
        /// <returns>True if it can be versioned</returns>
        public abstract bool Versionable(Type containerType);
        /// <summary>
        /// Whether a container can have a version in this system
        /// </summary>
        /// <param name="container">The container object</param>
        /// <returns>True if it can be versioned</returns>
        public virtual bool Versionable(object container)
        {
            bool contentVersionable = this.IsVersionable == null || this.IsVersionable(Collator.GetContentType(container));
            return Versionable(container.GetType()) && contentVersionable;
        }

        /// <summary>
        /// Set the current value for this versioning system using supplied mode on supplied ItemVersion
        /// </summary>
        /// <param name="mode">The mode to use</param>
        /// <param name="version">The ItemVersion on which to add the key/value for this versioning system</param>
        public abstract void SetCurrentVersion(VersioningMode mode, ItemVersion version);
        /// <summary>
        /// Get the versioning system value from a container and set this value on the supplied ItemVersion
        /// </summary>
        /// <param name="container">The container from which to get the version key</param>
        /// <param name="version">The ItemVersion on which to add the key/value for this versioning system</param>
        public abstract void GetItemVersion(object container, ItemVersion version);
        /// <summary>
        /// Set the versioning on a container to have the value from this versioning system from the supplied ItemVersion
        /// </summary>
        /// <param name="version">The ItemVersion from which to get the versioning value</param>
        /// <param name="container">The container on which to set the versioning value</param>
        public abstract void SetItemVersion(ItemVersion version, object container);
        /// <summary>
        /// Get display details for displaying the value for this versioning system from the supplied ItemVersion
        /// </summary>
        /// <param name="version">The ItemVersion for which to get display information for this versioning system</param>
        /// <returns>Display information for version from this versioning system</returns>
        public abstract VersionDisplay DisplayItemVersion(ItemVersion version);
        /// <summary>
        /// Test whether the given container's version in this versioning system is included in the supplied VersioningMode
        /// </summary>
        /// <param name="container">The container from which to get the version</param>
        /// <param name="mode">The VersioningMode for which we should run the test for this versioning system</param>
        /// <returns>Whether in this versioning system the container is part of the VersioningMode</returns>
        public abstract bool TestVersioningMode(object container, VersioningMode mode);
        /// <summary>
        /// Taking a url, add to it any switches or changes in this versioning system required for it to refer to a content
        /// item with the versioning value contained in the supplied ItemVersion for this versioning system.  This will
        /// leave the url unchanged for 'unaddressable' version keys
        /// </summary>
        /// <param name="url">The initial url</param>
        /// <param name="version">The version for which we are modifying the url</param>
        /// <returns>The modified url</returns>
        public virtual string GetVersionUrl(string url, ItemVersion version)
        {
            return url;
        }
        /// <summary>
        /// Get the version values which can be viewed by the supplied user for this version key
        /// </summary>
        /// <param name="u">The user</param>
        /// <returns>List of version values which the user can view</returns>
        public abstract List<object> GetAllowedVersions(IUser u);

    }
}
