using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Lynicon.Collation;
using Lynicon.Membership;
using Lynicon.Models;
using Lynicon.Repositories;
using Lynicon.Utility;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// Different ways of specifying a version which apply across all versioning systems
    /// </summary>
    public enum VersioningMode
    {
        /// <summary>
        /// Default - use only the current version of any content
        /// </summary>
        Current,
        /// <summary>
        /// Use a specified specific version - the specific version may indicate a set of possible versions
        /// </summary>
        Specific,
        /// <summary>
        /// Use the version(s) which could be seen by an anonymous user
        /// </summary>
        Public,
        /// <summary>
        /// Use all versions of any content
        /// </summary>
        All
    }

    /// <summary>
    /// Class for describing the display of a value of a versioning key in the UI
    /// </summary>
    public class VersionDisplay
    {
        /// <summary>
        /// The text to show for the version value in the small version indicator in the Function panel
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// The CSS class to apply to the version indicator
        /// </summary>
        public string CssClass { get; set; }
        /// <summary>
        /// The title attribute (shown on hover) to allow a longer description to be available
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The text to show in a dropdown list of version values
        /// </summary>
        public string ListItem { get; set; }
    }

    /// <summary>
    /// Globally available methods for working with the versioning system
    /// </summary>
    public class VersionManager
    {
        /// <summary>
        /// A stored state of the versioning system determining how the current version should be determined
        /// </summary>
        protected class VersioningState
        {
            /// <summary>
            /// A version to use in Specific versioning mode
            /// </summary>
            public ItemVersion SpecificVersion { get; set; }
            /// <summary>
            /// The versioning mode to use to get the current version
            /// </summary>
            public VersioningMode Mode { get; set; }
        }

        static readonly VersionManager instance = new VersionManager();
        public static VersionManager Instance { get { return instance; } }

        static VersionManager() { }

        protected List<Versioner> Versioners { get; set; }

        /// <summary>
        /// The version keys in the system where the value of the key is determinable from the differing urls of the versions
        /// implying these different versions are simultaneously accessible
        /// </summary>
        public List<string> AddressableVersionKeys { get; set; }
        /// <summary>
        /// The version keys in the system where the different versions of the same content item with different values of the key
        /// are all accessed via the same url implying only one version is accessible at once
        /// </summary>
        public List<string> UnaddressableVersionKeys { get; set; }

        /// <summary>
        /// Lists of possible values for each version key
        /// </summary>
        public Dictionary<string, object[]> VersionLists { get; set; }

        protected const string StateStackKey = "lyn_version_state_stack";
        /// <summary>
        /// Stack of versioning states.  Note this is held on the RequestThreadCache so a different stack is maintained for
        /// each thread of execution
        /// </summary>
        protected Stack<VersioningState> StateStack
        {
            get
            {
                if (!RequestThreadCache.Current.ContainsKey(StateStackKey))
                    RequestThreadCache.Current.Add(StateStackKey, new Stack<VersioningState>());
                return (Stack<VersioningState>)RequestThreadCache.Current[StateStackKey];
            }
        }

        protected const string ModeKey = "lyn_version_mode";
        /// <summary>
        /// Get or set the current versioning mode, applies only to the current thread of execution
        /// </summary>
        public VersioningMode Mode
        {
            get
            {
                if (!RequestThreadCache.Current.ContainsKey(ModeKey))
                    return VersioningMode.Current;
                return (VersioningMode)RequestThreadCache.Current[ModeKey];
            }
            set
            {
                if (!RequestThreadCache.Current.ContainsKey(ModeKey))
                    RequestThreadCache.Current.Add(ModeKey, value);
                else
                    RequestThreadCache.Current[ModeKey] = value;
            }
        }

        protected const string SpecificVersionKey = "lyn_version_specific";
        private ItemVersion specificVersion = new ItemVersion();
        /// <summary>
        /// Get or set the specific version which is used as the current version in
        /// VersioningMode.Specific
        /// </summary>
        public ItemVersion SpecificVersion
        {
            get
            {
                if (!RequestThreadCache.Current.ContainsKey(SpecificVersionKey))
                    return new ItemVersion();
                return (ItemVersion)RequestThreadCache.Current[SpecificVersionKey];
            }
            set
            {
                if (!RequestThreadCache.Current.ContainsKey(SpecificVersionKey))
                    RequestThreadCache.Current.Add(SpecificVersionKey, value);
                else
                    RequestThreadCache.Current[SpecificVersionKey] = value;
            }
        }

        public const string VersionOverrideKey = "Lynicon_VOver";
        /// <summary>
        /// Indirectly retrieves or sets version keys with which to override the current version
        /// stored on a cookie as set up by the current user by changing the version controller UI
        /// </summary>
        public ItemVersion ClientVersionOverride
        {
            get
            {
                var vOver = RequestContextManager.Instance.CurrentContext?.Request.Cookies[VersionOverrideKey];
                if (vOver == null)
                    return new ItemVersion();
                else
                    return new ItemVersion(vOver);
            }
            set
            {
                var resp = RequestContextManager.Instance.CurrentContext.Response;
                if (resp.GetCookie(VersionOverrideKey) == null)
                    resp.Cookies.Append(VersionOverrideKey, value.ToString());
                currentVersionInvalidated = true;
            }
        }

        private const string overrideBlockKey = "_lyn_overrideBlock";
        private bool OverrideVersion(ItemVersion v, ItemVersion over)
        {
            bool changed = false;

            // detected if we have recursed into this (on the current thread/request) and don't apply override if so
            bool blocked = (bool?)RequestThreadCache.Current[overrideBlockKey] ?? false;

            if (over.Count > 0 && !blocked)
            {
                IUser u = null;
                RequestThreadCache.Current[overrideBlockKey] = true;
                try
                {
                    u = SecurityManager.Current?.User;
                }
                finally
                {
                    RequestThreadCache.Current[overrideBlockKey] = false;
                }

                foreach (var versioner in Versioners)
                {
                    if (over.ContainsKey(versioner.VersionKey))
                    {
                        var legalVals = u == null
                            ? new List<object> { versioner.PublicVersionValue }
                            : versioner.GetAllowedVersions(u);
                        var overVal = over[versioner.VersionKey];
                        if (legalVals.Any(lVal => lVal.Equals(overVal)))
                        {
                            if (!v.ContainsKey(versioner.VersionKey)
                                || (v[versioner.VersionKey] == null && overVal != null)
                                || !v[versioner.VersionKey].Equals(overVal))
                            {
                                v[versioner.VersionKey] = overVal;
                                changed = true;
                            }
                        }
                    }
                }
            }

            return changed;
        }

        // Current version is cached on the request
        protected const string CurrentVersionKey = "lyn_version_current";

        bool currentVersionInvalidated = false;
        /// <summary>
        /// Get the current version: this is determined by the Versioner.SetCurrentVersion for each versioning
        /// system, then modified by the current VersioningMode and overriden by the current user's version
        /// override settings
        /// </summary>
        public ItemVersion CurrentVersion
        {
            get
            {
                if (Mode != VersioningMode.Current
                    || currentVersionInvalidated
                    || !RequestThreadCache.Current.ContainsKey(CurrentVersionKey))
                {
                    ItemVersion iv = null;
                    if (Mode == VersioningMode.All)
                        iv = new ItemVersion();
                    else if (Mode == VersioningMode.Specific)
                        iv = SpecificVersion;
                    else if (Mode == VersioningMode.Public)
                    {
                        iv = new ItemVersion();
                        foreach (var versioner in Versioners)
                            iv.Add(versioner.VersionKey, versioner.PublicVersionValue);
                    }
                    else
                    {
                        iv = new ItemVersion();
                        foreach (var versioner in Versioners)
                            versioner.SetCurrentVersion(Mode, iv);
                        var vOver = this.ClientVersionOverride;
                        if (vOver != null && vOver != iv)
                        {
                            currentVersionIsOverridden = OverrideVersion(iv, vOver);
                        } 
                        else
                            currentVersionIsOverridden = false;
                    }

                    if (Mode == VersioningMode.Current)
                        RequestThreadCache.Current[CurrentVersionKey] = new ItemVersion(iv);

                    return iv;
                }
                return new ItemVersion((ItemVersion)RequestThreadCache.Current[CurrentVersionKey]);
            }
        }

        bool currentVersionIsOverridden = false;
        /// <summary>
        /// Test for whether the user's settings have changed the current version at all, used to show
        /// feedback to the user
        /// </summary>
        public bool CurrentVersionIsOverridden
        {
            get
            {
                var v = this.CurrentVersion;
                return currentVersionIsOverridden;
            }
        }

        /// <summary>
        /// Get the current version with only the keys applicable to a type
        /// </summary>
        /// <param name="type">The type to which the keys must be applicable</param>
        /// <returns>Current Item version with only the keys applicable to a type</returns>
        public ItemVersion CurrentVersionForType(Type type)
        {
            var vsn = new ItemVersion(CurrentVersion);
            foreach (var versioner in Versioners)
                if (!versioner.Versionable(type))
                    vsn.Remove(versioner.VersionKey);

            return vsn;
        }

        /// <summary>
        /// A current version may contain null for a version key leaving it undefined.  This indicates the preferred version
        /// is the first existing version built by defining the undefined version key with each of the possible values in VersionLists[key] in
        /// order.  This maps this rule across all keys to produce a list of versions to check for existence in order.
        /// </summary>
        /// <param name="type">Each item version must only have the keys applicable to this type</param>
        /// <returns>List of fully-defined versions to check in order</returns>
        public List<ItemVersion> CurrentVersions(Type type)
        {
            var currentV = CurrentVersionForType(type);
            return currentV.Expand();
        }
        /// <summary>
        /// A current version may contain null for a version key leaving it undefined.  This indicates the preferred version
        /// is the first existing version built by defining the undefined version key with each of the possible values in VersionLists[key] in
        /// order.  This maps this rule across all keys to produce a list of versions to check for existence in order.
        /// </summary>
        /// <returns>List of fully-defined versions to check in order</returns>
        public List<ItemVersion> CurrentVersions()
        {
            var currentV = CurrentVersion;
            return currentV.Expand();
        }

        /// <summary>
        /// The public version in the current versioning configuration.  Note this can include null-valued keys
        /// for addressable versions because all those versions will be public.
        /// </summary>
        public ItemVersion PublicVersion
        {
            get
            {
                var iv = new ItemVersion();
                foreach (var versioner in Versioners)
                    iv.Add(versioner.VersionKey, versioner.PublicVersionValue);
                return iv;
            }
        }

        /// <summary>
        /// Expand the abstract version to a list of fully-specified versions
        /// with all the keys of all the registered versions
        /// </summary>
        /// <param name="iv">Abstract version to expand</param>
        /// <returns>List of fully-specified versions</returns>
        public List<ItemVersion> ContainingVersions(ItemVersion iv)
        {
            var expIv = new ItemVersion(iv);
            foreach (var versioner in Versioners)
            {
                if (!iv.ContainsKey(versioner.VersionKey))
                    expIv.Add(versioner.VersionKey, null);
            }
            return expIv.Expand();
        }

        /// <summary>
        /// Create a version manager
        /// </summary>
        public VersionManager()
        {
            Versioners = new List<Versioner>();
            AddressableVersionKeys = new List<string>();
            UnaddressableVersionKeys = new List<string>();
            VersionLists = new Dictionary<string, object[]>();
        }

        /// <summary>
        /// Register a versioner to add a versioning system to the VersionManager
        /// </summary>
        /// <param name="name">The version key</param>
        /// <param name="versioner">The Versioner for the versioning system</param>
        public void RegisterVersion(Versioner versioner)
        {
            Versioners.Add(versioner);
            if (versioner.IsAddressable)
                AddressableVersionKeys.Add(versioner.VersionKey);
            else
                UnaddressableVersionKeys.Add(versioner.VersionKey);
            VersionLists.Add(versioner.VersionKey, versioner.AllVersionValues);
        }

        /// <summary>
        /// Push a versioning state just including a VersioningMode (making it active)
        /// </summary>
        /// <param name="mode">The VersioningMode to push</param>
        public void PushState(VersioningMode mode)
        {
            PushState(mode, null);
        }
        /// <summary>
        /// Push a versioning state with a VersioningMode and a specific ItemVersion (making it active)
        /// </summary>
        /// <param name="mode">The VersioningMode to push</param>
        /// <param name="specificVersion">The SpecificVersion to push</param>
        public void PushState(VersioningMode mode, ItemVersion specificVersion)
        {
            StateStack.Push(new VersioningState { Mode = this.Mode, SpecificVersion = this.SpecificVersion });
            this.Mode = mode;
            if (specificVersion != null)
                this.SpecificVersion = specificVersion;
        }

        /// <summary>
        /// Pop the current versioning state, returning the state to the one before it was pushed
        /// </summary>
        public void PopState()
        {
            var state = StateStack.Pop();
            this.Mode = state.Mode;
            this.SpecificVersion = state.SpecificVersion;
        }

        /// <summary>
        /// Get the ItemVersion of a container
        /// </summary>
        /// <param name="container">The container</param>
        /// <returns>The ItemVersion of the container</returns>
        public ItemVersion GetVersion(object container)
        {
            var version = new ItemVersion();
            foreach (var versioner in Versioners)
                if (versioner.Versionable(container))
                    versioner.GetItemVersion(container, version);

            return version;
        }

        /// <summary>
        /// Set the container to have the ItemVersion supplied
        /// </summary>
        /// <param name="version">ItemVersion to set on the container</param>
        /// <param name="container">The container to set the ItemVersion for</param>
        public void SetVersion(ItemVersion version, object container)
        {
            foreach (var versioner in Versioners)
                if (version.ContainsKey(versioner.VersionKey) && versioner.Versionable(container))
                    versioner.SetItemVersion(version, container);
        }

        /// <summary>
        /// Test whether the version of the container fits the VersioningMode given
        /// </summary>
        /// <param name="container">The container whose version is to be tested</param>
        /// <param name="mode">The VersioningMode to test the container against</param>
        /// <returns>True if the version fits the VersioningMode</returns>
        public bool TestVersioningMode(object container, VersioningMode mode)
        {
            if (mode == VersioningMode.All)
                return true;

            foreach (var versioner in Versioners)
                if (versioner.Versionable(container) && !versioner.TestVersioningMode(container, mode))
                    return false;

            return true;
        }

        /// <summary>
        /// Get all the displays for all the keys in an ItemVersion
        /// </summary>
        /// <param name="version">The ItemVersion to display</param>
        /// <returns>The VersionDisplays for all the keys of the ItemVersion</returns>
        public List<VersionDisplay> DisplayVersion(ItemVersion version)
        {
            return Versioners
                .OrderBy(v => v.VersionKey)
                .Where(v => version.ContainsKey(v.VersionKey))
                .Select(v => v.DisplayItemVersion(version))
                .Where(vi => vi != null && !string.IsNullOrEmpty(vi.Text))
                .ToList();
        }

        /// <summary>
        /// Create an ItemVersion with null values for all the keys applicable to a type
        /// </summary>
        /// <param name="type">The type for which to create the ItemVersion</param>
        /// <returns>ItemVersion with null values for all the keys applicable</returns>
        public ItemVersion VersionForType(Type type)
        {
            var iv = new ItemVersion();
            foreach (var versioner in Versioners)
                if (versioner.Versionable(type))
                    iv.Add(versioner.VersionKey, null);
            return iv;
        }

        /// <summary>
        /// Whether a given version can be viewed by a given user
        /// </summary>
        /// <param name="u">The user</param>
        /// <param name="version">The ItemVersion</param>
        /// <returns>Whether the ItemVersion can be viewed by the user</returns>
        public bool VersionPermitted(IUser u, ItemVersion version)
        {
            foreach (var versioner in Versioners)
                if (!versioner.GetAllowedVersions(u).Contains(version[versioner.VersionKey]))
                    return false;

            return true;
        }

        /// <summary>
        /// Get a list of VersionSelectionViewModels to create a version selector for a given user and a given current version
        /// </summary>
        /// <param name="u">The user</param>
        /// <param name="currVersion">The current version</param>
        /// <returns>List of VersionSelectionViewModels</returns>
        public List<VersionSelectionViewModel> SelectionViewModel(IUser u, ItemVersion currVersion)
        {
            return SelectionViewModel(u, currVersion, false);
        }
        /// <summary>
        /// Get a list of VersionSelectionViewModels to create a version selector for a given user and a given current version
        /// </summary>
        /// <param name="u">The user</param>
        /// <param name="currVersion">The current version</param>
        /// <param name="abbreviated">Whether the display is abbreviated (not implemented)</param>
        /// <returns>List of VersionSelectionViewModels</returns>
        public List<VersionSelectionViewModel> SelectionViewModel(IUser u, ItemVersion currVersion, bool abbreviated)
        {
            var vm = new List<VersionSelectionViewModel>();
            foreach (var v in Versioners.Where(v => currVersion.ContainsKey(v.VersionKey) && currVersion[v.VersionKey] != null).OrderBy(v => v.VersionKey))
            {
                var versions = v.GetAllowedVersions(u);
                var selectList = new List<SelectListItem>();
                string cssClass = null;
                foreach (object o in versions)
                {
                    var iv = new ItemVersion { { v.VersionKey, o } };
                    var dv = v.DisplayItemVersion(iv);
                    cssClass = dv.CssClass;
                    var sli = new SelectListItem();
                    sli.Text = (abbreviated ? dv.Text + "|" : "") + dv.ListItem;
                    sli.Value = JsonConvert.SerializeObject(o);
                    sli.Selected = currVersion[v.VersionKey] != null && currVersion[v.VersionKey].Equals(o);
                    selectList.Add(sli);
                }
                vm.Add(new VersionSelectionViewModel
                    {
                        Title = v.VersionKey,
                        SelectList = selectList,
                        VersionKey = v.VersionKey,
                        CssClass = cssClass
                    });
            }

            return vm;
        }

        /// <summary>
        /// Get the url to access a specific version of a content item given a base url
        /// </summary>
        /// <param name="currUrl">The base url</param>
        /// <param name="version">The version for which to modify the url to access</param>
        /// <returns>Modified url</returns>
        public string GetVersionUrl(string currUrl, ItemVersion version)
        {
            string url = currUrl;
            foreach (var versioner in Versioners)
            {
                url = versioner.GetVersionUrl(url, version);
            }
            return url;
        }
        
        /// <summary>
        /// Make the ItemVersion applicable to a given type by removing any inapplicable keys
        /// </summary>
        /// <param name="version">The original ItemVersion</param>
        /// <param name="t">The type to which to make it applicable</param>
        /// <returns>The modified, applicable ItemVersion</returns>
        public ItemVersion GetApplicableVersion(ItemVersion version, Type t)
        {
            ItemVersion res = new ItemVersion();
            foreach (var versioner in Versioners)
            {
                if (!version.ContainsKey(versioner.VersionKey))
                    continue;
                if (versioner.Versionable(t))
                    res.Add(versioner.VersionKey, version[versioner.VersionKey]);
            }
            return res;
        }

        /// <summary>
        /// Describe the current state of the version stack for the current thread
        /// </summary>
        /// <returns>Current state of the version stack described as a string</returns>
        internal string StackDescribe()
        {
            StringBuilder sb = new StringBuilder();
            foreach (VersioningState state in StateStack)
            {
                sb.AppendLine(string.Format("{0} {1}", state.Mode, state.SpecificVersion));
            }

            return sb.ToString();
        }
    }
}
