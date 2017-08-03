using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Extensibility;
using Lynicon.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lynicon.Models;
using Lynicon.Extensions;
using Lynicon.Commands;

namespace Lynicon.Startup
{
    public static class StartupX
    {
        /// <summary>
        /// Add services for Lynicon
        /// </summary>
        /// <param name="services">The services collection</param>
        /// <param name="optionsAction">Action to build options</param>
        /// <returns>LyniconSystemBuilder for chaining</returns>
        public static LyniconSystemBuilder AddLynicon(this IServiceCollection services, Action<LyniconSystemOptions> optionsAction)
        {
            var options = new LyniconSystemOptions();
            optionsAction(options);
            var lynSystem = new LyniconSystem(options);
            lynSystem.SetAsPrimarySystem();
            LyniconSystem.Instance = lynSystem;
            services.AddSingleton<LyniconSystem>(lynSystem);
            services.AddSingleton<IAuthorizationHandler, ContentPermissionHandler>();
            services.AddSingleton<ICommandRunner, CommandRunner>();
            return new LyniconSystemBuilder(lynSystem, services);
        }

        /// <summary>
        /// Set up authorization for Lynicon with overrides to the standard setup
        /// </summary>
        /// <param name="authOptions">Authorization options to which to add Lynicon authorization</param>
        /// <param name="permissionOverrides">Dictionary of Lynicon internal policy names ("CanEditData", "CanDeleteData") with ContentPermission objects to set the required behaviour</param>
        /// <returns></returns>
        public static AuthorizationOptions AddLyniconAuthorization(this AuthorizationOptions authOptions, Dictionary<string, ContentPermission> permissionOverrides)
        {
            var canEditDataPermission = permissionOverrides.ContainsKey("CanEditData")
                ? permissionOverrides["CanEditData"]
                : new ContentPermission("E");
            authOptions.AddPolicy("CanEditData", policy => policy.AddRequirements(canEditDataPermission));

            var canDeleteDataPermission = permissionOverrides.ContainsKey("CanDeleteData")
                ? permissionOverrides["CanDeleteData"]
                : new ContentPermission("A");
            authOptions.AddPolicy("CanDeleteData", policy => policy.AddRequirements(canDeleteDataPermission));

            return authOptions;
        }
        /// <summary>
        /// Set up standard authorization for Lynicon
        /// </summary>
        /// <param name="authOptions">Authorization options to which to add Lynicon authorization</param>
        /// <returns></returns>
        public static AuthorizationOptions AddLyniconAuthorization(this AuthorizationOptions authOptions)
        {
            authOptions.AddLyniconAuthorization(new Dictionary<string, ContentPermission>());
            return authOptions;
        }

        /// <summary>
        /// Set up Lynicon, creating required objects before defining routing
        /// </summary>
        /// <param name="app">Application builder on which to set up Lynicon</param>
        /// <returns>Application builder with Lynicon set up</returns>
        public static IApplicationBuilder ConstructLynicon(this IApplicationBuilder app)
        {
            app.ApplicationServices.GetService<LyniconSystem>().Construct(app);
            return app;
        }

        /// <summary>
        /// Turn off checking versioning information for modules stored in database
        /// </summary>
        /// <param name="app">Application builder on which to turn off checking</param>
        /// <returns>Application builder with checking turned off</returns>
        public static IApplicationBuilder LyniconSuppressDbVerification(this IApplicationBuilder app)
        {
            app.ApplicationServices.GetService<LyniconSystem>().Modules.SkipDbStateCheck = true;
            return app;
        }

        /// <summary>
        /// Initialise Lynicon after routing has been set up
        /// </summary>
        /// <param name="app">Application builder on which to initialise Lynicon</param>
        /// <param name="life">Application lifetime manager to which Lynicon will attach event handlers</param>
        /// <returns>Application builder with Lynicon initialised</returns>
        public static IApplicationBuilder InitialiseLynicon(this IApplicationBuilder app, IApplicationLifetime life)
        {
            app.ApplicationServices.GetService<LyniconSystem>().Initialise(app, life);
            return app;
        }

        /// <summary>
        /// Add MVC customisations for Lynicon to MvcOptions
        /// </summary>
        /// <param name="options">MvcOptions to which to add Lynicon customisations</param>
        /// <returns>MvcOptions with Lynicon customisations added</returns>
        public static MvcOptions AddLyniconOptions(this MvcOptions options)
        {
            options.ModelBinderProviders.Insert(0, new ContentTypeModelBinderProvider());
            options.ModelBinderProviders.Insert(0, new PolymorphicModelBinderProvider());
            options.ModelMetadataDetailsProviders.Add(new MetadataAwareMetadataProvider());
            return options;
        }
    }
}
