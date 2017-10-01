using Lynicon.Membership;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using Lynicon.Collation;
using Lynicon.Repositories;
using Lynicon.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore;
using Lynicon.Extensibility;
using System.Security.Claims;
using Lynicon.DataSources;
using Lynicon.Services;

namespace Lynicon.AspNetCore.Identity
{
    /// <summary>
    /// An ISecurityManager to work with ASP.Net Identity
    /// </summary>
    /// <typeparam name="TContext">The client user database context type (e.g. ApplicationDbContext)</typeparam>
    /// <typeparam name="TUser">The client user record type (e.g. ApplicationUser)</typeparam>
    public class IdentityAdaptorSecurityManager<TUser, TKey, TContext, TUserManager, TSignInManager> : ISecurityManager
        where TUser : IdentityUser, new()
        where TContext : IdentityDbContext<TUser>
        where TUserManager : UserManager<TUser>
        where TKey : IEquatable<TKey>
        where TSignInManager : SignInManager<TUser>
    {
        public const string CurrentUserKey = "_lyn_idapsecm_user";

        Func<UserManager<TUser>> getUserManager;
        Func<SignInManager<TUser>> getSignInManager;
        Func<TContext> getContext;

        public IdentityAdaptorSecurityManager(
            LyniconSystem sys,
            Func<TContext> getContext,
            Func<UserManager<TUser>> getUserManager,
            Func<SignInManager<TUser>> getSignInManager)
        {
            this.getContext = getContext;
            this.getUserManager = getUserManager;
            this.getSignInManager = getSignInManager;
        }

        /// <summary>
        /// Configure the data system to map requests for User type onto TUser via Identity mechanisms
        /// </summary>
        public virtual void InitialiseDataApi()
        {
            ContentTypeHierarchy.RegisterType(typeof(User));
            ContentTypeHierarchy.RegisterType(typeof(TUser));

            var sys = LyniconSystem.Instance;

            var extender = LyniconSystem.Instance.Extender;
            extender.RegisterForExtension(typeof(User));
            //extender.RegisterExtensionType(typeof(LyniconIdentityUser));

            var efDSFactory = new EFDataSourceFactory<TContext>(sys);
            var appDbRepository = new BasicRepository(sys, efDSFactory);
            efDSFactory.DbSetSelectors[typeof(TUser)] = db => db.Users.AsNoTracking();
            efDSFactory.ContextLifetimeMode = ContextLifetimeMode.PerCall;

            // We DON'T want to register TUser with CompositeTypeManager
            var basicCollator = new BasicCollator(sys);
            Collator.Instance.Register(typeof(TUser), new BasicCollator(sys));
            Repository.Instance.Register(typeof(TUser), appDbRepository);

            // override existing collator registration for User
            var identityAdaptorCollator = new IdentityAdaptorCollator<TUser, TUserManager>(sys);
            Collator.Instance.Register(typeof(User), identityAdaptorCollator);
            //Repository.Instance.Register(typeof(User), new BasicRepository());
        }

        private async Task<LyniconIdentityUser> GetUserAsync(string userId)
        {
            LyniconIdentityUser lynUser = null;
            try
            {
                var um = (UserManager<TUser>)RequestContextManager.Instance.CurrentContext.RequestServices.GetService(typeof(UserManager<TUser>));
                TUser user = await um.FindByIdAsync(userId);
                lynUser = new LyniconIdentityUser
                {
                    IdAsString = user.Id,
                    UserName = user.UserName,
                    Email = await um.GetEmailAsync(user),
                    Roles = new string(
                           (await um.GetRolesAsync(user))
                           .Where(r => r.Length == 1)
                           .Select(r => r[0])
                           .ToArray())
                };
            }
            catch
            { }

            return lynUser;
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

                    // have to call async synchronously to preserve interface
                    reqItems[CurrentUserKey] = Task<LyniconIdentityUser>.Run(() => GetUserAsync(userId)).Result;
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
                    var sm = (SignInManager<TUser>)RequestContextManager.Instance.CurrentContext.RequestServices.GetService(typeof(SignInManager<TUser>));
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
                var sm = (SignInManager<TUser>)RequestContextManager.Instance.CurrentContext.RequestServices.GetService(typeof(SignInManager<TUser>));
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
            return Task.Run(() => SetPasswordAsync(userId, newPw)).Result;
        }

        private async Task<bool> SetPasswordAsync(string userId, string newPw)
        {
            try
            {
                UserStore<TUser> store = new UserStore<TUser>(this.getContext());
                var pwHasher = new PasswordHasher<TUser>();
                TUser cUser = await store.FindByIdAsync(userId);
                String hashedNewPassword = pwHasher.HashPassword(cUser, newPw);
                await store.SetPasswordHashAsync(cUser, hashedNewPassword);
                await store.UpdateAsync(cUser);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void EnsureRoles(string roles)
        {
            // sort this out

            //var roleStore = new RoleStore<IdentityRole>(getContext());
            //var roleManager = new RoleManager<IdentityRole>(roleStore);
            //foreach (string role in roles.ToCharArray().Select(c => c.ToString()))
            //{
            //    if (!roleManager.RoleExists(role))
            //        roleManager.Create(new IdentityRole(role) { Id = role });
            //}
        }
    }
}