using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;
using Lynicon.Utility;
using System.Threading;
using System.Security.Claims;
using Lynicon.Collation;
using Lynicon.Models;
using Lynicon.Repositories;
using Lynicon.DataSources;
using System.Threading.Tasks;
using Lynicon.Extensibility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Lynicon.Services;

namespace Lynicon.Membership
{
    /// <summary>
    /// An ISecurityManager to work with ASP.Net Identity
    /// </summary>
    /// <typeparam name="TContext">The client user database context type (e.g. ApplicationDbContext)</typeparam>
    /// <typeparam name="TUser">The client user record type (e.g. ApplicationUser)</typeparam>
    public class LyniconIdentitySecurityManager : ISecurityManager
    {
        public const string CurrentUserKey = "_lyn_lynidsecm_user";

        Func<UserManager<User>> getUserManager;
        Func<SignInManager<User>> getSignInManager;

        public LyniconIdentitySecurityManager(
            Func<UserManager<User>> getUserManager,
            Func<SignInManager<User>> getSignInManager)
        {
            this.getUserManager = getUserManager;
            this.getSignInManager = getSignInManager;
        }

        /// <summary>
        /// Configure the data system to map requests for User type onto TUser via Identity mechanisms
        /// </summary>
        public virtual void InitialiseDataApi()
        {
            ContentTypeHierarchy.RegisterType(typeof(User));
            var sys = LyniconSystem.Instance;
            sys.Extender.RegisterForExtension(typeof(User));

            Collator.Instance.Register(typeof(User), new BasicCollator(sys));
            Repository.Instance.Register(typeof(User), new BasicRepository(sys, new CoreDataSourceFactory(sys)));
        }

        /// <summary>
        /// The current user (cached on request)
        /// </summary>
        public Lynicon.Membership.IUser User
        {
            get
            {
                if (RequestContextManager.Instance?.CurrentContext == null)
                    return null;

                var reqItems = RequestContextManager.Instance.CurrentContext.Items;
                if (reqItems[CurrentUserKey] == null)
                {
                    var userId = this.UserId;
                    if (userId == null)
                        return null;

                    using (var ctx = VersionManager.Instance.PushState(VersioningMode.Public))
                    {
                        reqItems[CurrentUserKey] = Collator.Instance.Get<User>(new ItemId(typeof(User), userId));
                    } 
                }
                return (IUser)reqItems[CurrentUserKey];
            }
        }

        /// <summary>
        /// The userid of the current user
        /// </summary>
        public string UserId
        {
            get
            {
                var user = RequestContextManager.Instance.CurrentContext.User;
                if (!user.Identity.IsAuthenticated)
                    return null;
                return user.FindFirst(ClaimTypes.NameIdentifier).Value;
            }
        }

        /// <summary>
        /// Whether the current user is in a given role
        /// </summary>
        /// <param name="role">The role (a single letter)</param>
        /// <returns>True if the user is in the role</returns>
        public bool CurrentUserInRole(string role)
        {
            var user = User;
            if (user == null)
                return false;
            return user.Roles.Contains(role);
        }

        /// <summary>
        /// Try to log in a user with an option to persist the login cookie
        /// </summary>
        /// <param name="username">The user's username</param>
        /// <param name="password">The user's password</param>
        /// <param name="keepLoggedIn">True to persist the login</param>
        /// <returns>The user record or null if failed</returns>
        public Lynicon.Membership.IUser LoginUser(string username, string password, bool keepLoggedIn)
        {
            var result = Task<SignInResult>.Run(
                () =>
                {
                    var sm = (SignInManager<User>)RequestContextManager.Instance.CurrentContext.RequestServices.GetService(typeof(SignInManager<User>));
                    return sm.PasswordSignInAsync(username, password, keepLoggedIn, false);
                }
                ).Result;
            if (result.Succeeded)
                return Collator.Instance.Get<User, User>(iq => iq.Where(u => u.UserName == username)).FirstOrDefault();

            return null;
        }

        /// <summary>
        /// Log out the current user
        /// </summary>
        public void Logout()
        {
            var x = Task.Run(() =>
            {
                var sm = (SignInManager<User>)RequestContextManager.Instance.CurrentContext.RequestServices.GetService(typeof(SignInManager<User>));
                sm.SignOutAsync();
                return true;
            }).Result;
        }

        /// <summary>
        /// Set the password for the current user (beware, security risk if misused)
        /// </summary>
        /// <param name="userId">Id of the user</param>
        /// <param name="newPw">The new password</param>
        /// <returns>True if succeeded</returns>
        public bool SetPassword(string userId, string newPw)
        {
            var user = Collator.Instance.Get<User>(new ItemId(typeof(User), userId));
            var pwHasher = new PasswordHasher<User>();
            string hashedNewPassword = pwHasher.HashPassword(user, newPw);
            user.Password = hashedNewPassword;
            Collator.Instance.Set(user);
            return true;
        }

        public void EnsureRoles(string roles)
        {
            // TO DO make this work with services

            //var roleStore = new RoleStore<IdentityRole>(new IdentityDbContext());
            //var roleManager = new RoleManager<IdentityRole>(roleStore);
            //foreach (string role in roles.ToCharArray().Select(c => c.ToString()))
            //{
            //    if (!roleManager.RoleExists(role))
            //        roleManager.Create(new IdentityRole(role) { Id = role });
            //}
        }
    }
}
