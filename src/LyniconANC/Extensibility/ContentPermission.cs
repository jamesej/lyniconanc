using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Collation;
using Lynicon.Membership;
using Lynicon.Models;
using Lynicon.Repositories;
using Lynicon.Utility;
using System.Collections;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Reflection;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// Describes the conditions under which content operations may be performed
    /// depending on the current user's roles and the content item itself
    /// </summary>
    public class ContentPermission : IAuthorizationRequirement
    {
        /// <summary>
        /// Function which returns true if the current roles and data item are permitted
        /// </summary>
        public Func<string, object, bool> TestPermitted { get; set; }

        /// <summary>
        /// Dictionary whose keys are version keys and whose values are lists of
        /// valid version values.  For every key in the content item's version, if that
        /// key is in the VersionMask, the value must match one of the values in the
        /// VersionMask
        /// </summary>
        public Dictionary<string, List<object>> VersionMask { get; set; }

        /// <summary>
        /// Create a ContentPermission that always permits the content item
        /// </summary>
        public ContentPermission()
        {
            TestPermitted = (r, d) => true;
            VersionMask = new Dictionary<string, List<object>>();
        }
        /// <summary>
        /// Create a ContentPermission that requires all the roles listed
        /// </summary>
        /// <param name="requiredRoles">string containing all the role letters required</param>
        public ContentPermission(string requiredRoles) : this()
        {
            TestPermitted = (roles, data) => requiredRoles.All(rc => roles.Contains(rc));
        }
        public ContentPermission(Func<string, object, bool> testPermitted) : this()
        {
            TestPermitted = testPermitted;
        }

        private string CurrentRoles()
        {
            IUser u = SecurityManager.Current?.User;
            if (u == null) return "";
            return u.Roles;
        }
        private string CurrentRoles(ClaimsPrincipal user)
        {
            return new string(user.Claims
                .Where(c => c.Type == ClaimTypes.Role && c.Value.Length == 1)
                .Select(c => c.Value[0])
                .ToArray());
        }

        private bool VersionPermitted(object content)
        {
            if (VersionMask.Keys.Count == 0)
                return true;

            Type type = null;
            if (content is IList)
                type = content.GetType().GetGenericArguments()[0];
            else if (content != null)
                type = content.GetType().ContentType();

            if (content == null || !ContentTypeHierarchy.AllContentTypes.Contains(type))
                return false;

            ItemVersion vsn = null;
            if (content is IList)
            {
                vsn = VersionManager.Instance.CurrentVersion;
            }
            else
            {
                var container = Collator.Instance.GetContainer(content);
                if (container == null)
                    return false;

                vsn = new ItemVersion(container);
            }

            foreach (var kvp in vsn)
            {
                if (VersionMask.ContainsKey(kvp.Key))
                    if (!VersionMask[kvp.Key].Contains(kvp.Value))
                        return false;
            }
            return true;
        }

        /// <summary>
        /// Test whether a given content item is permitted under this ContentPermission, getting
        /// current user from context
        /// </summary>
        /// <param name="content">The content item</param>
        /// <returns>True if permitted</returns>
        public bool Permitted(object content)
        {
            return Permitted(content, null);
        }
        /// <summary>
        /// Test whether a given content item is permitted under this ContentPermission
        /// </summary>
        /// <param name="content">The content item</param>
        /// <returns>True if permitted</returns>
        public bool Permitted(object content, ClaimsPrincipal user)
        {
            string roles = user == null ? CurrentRoles() : CurrentRoles(user);
            bool rolesDataOK = TestPermitted(roles, content);
            bool versionOK = VersionPermitted(content);
            return rolesDataOK && versionOK;
        }

    }
}
