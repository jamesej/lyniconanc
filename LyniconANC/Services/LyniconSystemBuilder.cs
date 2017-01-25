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
    public class LyniconSystemBuilder
    {
        LyniconSystem target;
        IServiceCollection services;

        public LyniconSystemBuilder(LyniconSystem target, IServiceCollection services)
        {
            this.target = target;
            this.services = services;
        }

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
