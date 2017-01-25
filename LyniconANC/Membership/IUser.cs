using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lynicon.Models;

namespace Lynicon.Membership
{
    /// <summary>
    /// The interface for a CMS user
    /// </summary>
    public interface IUser
    {
        /// <summary>
        /// The user's username
        /// </summary>
        string UserName { get; set; }

        string NormalisedUserName { get; set; }

        /// <summary>
        /// User's primary key id
        /// </summary>
        string IdAsString { get; set; }

        /// <summary>
        /// User's email
        /// </summary>
        string Email { get; set; }

        string NormalisedEmail { get; set; }

        /// <summary>
        /// When created
        /// </summary>
        DateTime Created { get; set; }

        /// <summary>
        /// When last modified
        /// </summary>
        DateTime Modified { get; set; }

        /// <summary>
        /// Password (as encrypted)
        /// </summary>
        string Password { get; set; }

        /// <summary>
        /// Roles as a string of role letters
        /// </summary>
        string Roles { get; set; }

        /// <summary>
        /// Test for whether logged in
        /// </summary>
        /// <returns>True if logged in</returns>
        bool IsLoggedIn();

        /// <summary>
        /// Test for whether the user is anonymous
        /// </summary>
        /// <returns>True is user is anonymous</returns>
        bool IsAnon();
    }
}
