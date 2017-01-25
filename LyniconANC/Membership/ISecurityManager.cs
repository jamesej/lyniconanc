using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Membership
{
    public interface ISecurityManager
    {
        /// <summary>
        /// The current user
        /// </summary>
        IUser User { get; }

        /// <summary>
        /// Whether the current user is in a given role
        /// </summary>
        /// <param name="role">The role (a single letter)</param>
        /// <returns>True if the user is in the role</returns>
        bool CurrentUserInRole(string role);

        /// <summary>
        /// The userid of the current user
        /// </summary>
        string UserId { get; }

        /// <summary>
        /// Try to log in a user with an option to persist the login cookie
        /// </summary>
        /// <param name="username">The user's username</param>
        /// <param name="password">The user's password</param>
        /// <param name="keepLoggedIn">True to persist the login</param>
        /// <returns>The user record or null if failed</returns>
        IUser LoginUser(string username, string password, bool keepLoggedIn);

        /// <summary>
        /// Log out the current user
        /// </summary>
        void Logout();

        /// <summary>
        /// Set the password for the current user (beware, security risk if misused)
        /// </summary>
        /// <param name="userId">Id of the user</param>
        /// <param name="newPw">The new password</param>
        /// <returns>True if succeeded</returns>
        bool SetPassword(string userId, string newPw);

        /// <summary>
        /// Ensure all the role letters in the roles string exist
        /// </summary>
        /// <param name="roles"></param>
        void EnsureRoles(string roles);

        /// <summary>
        /// Set up the Data Api to provide an adaptation layer to return the Lynicon User type
        /// </summary>
        void InitialiseDataApi();
    }
}
