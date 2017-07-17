using Lynicon.AspNetCore.Identity;
using Lynicon.Membership;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Services
{
    /// <summary>
    /// Fluent builder object for creating a Lynicon system
    /// </summary>
    public class LyniconSystemBuilder
    {
        LyniconSystem target;
        IServiceCollection services;

        public LyniconSystemBuilder(LyniconSystem target, IServiceCollection services)
        {
            this.target = target;
            this.services = services;
        }

        /// <summary>
        /// Set up user management via ASP.Net Identity integrated with Lynicon
        /// </summary>
        /// <typeparam name="TUser">Type of the user</typeparam>
        /// <typeparam name="TKey">Type of the user record key</typeparam>
        /// <typeparam name="TContext">Type of the DbContext used for persistence of user records</typeparam>
        /// <typeparam name="TUserManager">Type of the class providing functions for management of users</typeparam>
        /// <typeparam name="TSignInManager">Type of the class providing sign in functions</typeparam>
        /// <returns>Modified builder for a Lynicon system</returns>
        public LyniconSystemBuilder AddIdentityAdaptor<TUser, TKey, TContext, TUserManager, TSignInManager>()
            where TUser : IdentityUser, new()
            where TContext : IdentityDbContext<TUser>
            where TUserManager : UserManager<TUser>
            where TKey : IEquatable<TKey>
            where TSignInManager : SignInManager<TUser>
        {
            services.AddSingleton<Func<TContext>>(s => (() => s.GetService<TContext>()));
            services.AddSingleton<Func<UserManager<TUser>>>(s => (() => s.GetService<UserManager<TUser>>()));
            services.AddSingleton<Func<SignInManager<TUser>>>(s => (() => s.GetService<SignInManager<TUser>>()));
            services.AddSingleton<ISecurityManager, IdentityAdaptorSecurityManager<TUser, TKey, TContext, TUserManager, TSignInManager>>();
            return this;
        }

        /// <summary>
        /// Set up user management via the default customised version of ASP.Net Identity to work with Lynicon
        /// </summary>
        /// <returns>Modified builder for a Lynicon system</returns>
        public LyniconSystemBuilder AddLyniconIdentity()
        {
            services.AddSingleton<Func<UserManager<User>>>(s => (() => s.GetService<UserManager<User>>()));
            services.AddSingleton<Func<SignInManager<User>>>(s => (() => s.GetService<SignInManager<User>>()));

            services.AddScoped<IUserStore<User>, LyniconUserStore<User>>();
            services.AddScoped<IRoleStore<IdentityRole>, LyniconRoleStore>();

            services.AddSingleton<ISecurityManager, LyniconIdentitySecurityManager>();
            return this;
        }
    }
}
