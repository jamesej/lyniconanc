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
        public static LyniconSystemBuilder AddLynicon(this IServiceCollection services, Action<LyniconSystemOptions> optionsAction)
        {
            var options = new LyniconSystemOptions();
            optionsAction(options);
            var lynSystem = new LyniconSystem(options);
            LyniconSystem.Instance = lynSystem;
            services.AddSingleton<LyniconSystem>(lynSystem);
            services.AddSingleton<IAuthorizationHandler, ContentPermissionHandler>();
            services.AddSingleton<ICommandRunner, CommandRunner>();
            return new LyniconSystemBuilder(lynSystem, services);
        }

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
        public static AuthorizationOptions AddLyniconAuthorization(this AuthorizationOptions authOptions)
        {
            authOptions.AddLyniconAuthorization(new Dictionary<string, ContentPermission>());
            return authOptions;
        }

        public static IApplicationBuilder ConstructLynicon(this IApplicationBuilder app)
        {
            app.ApplicationServices.GetService<LyniconSystem>().Construct(app);
            return app;
        }

        public static IApplicationBuilder LyniconSuppressDbVerification(this IApplicationBuilder app)
        {
            app.ApplicationServices.GetService<LyniconSystem>().Modules.SkipDbStateCheck = true;
            return app;
        }

        public static IApplicationBuilder InitialiseLynicon(this IApplicationBuilder app, IApplicationLifetime life)
        {
            app.ApplicationServices.GetService<LyniconSystem>().Initialise(app, life);
            return app;
        }

        public static void AddLyniconOptions(this MvcOptions options)
        {
            options.ModelBinderProviders.Insert(0, new ContentTypeModelBinderProvider());
            options.ModelBinderProviders.Insert(0, new PolymorphicModelBinderProvider());
            options.ModelMetadataDetailsProviders.Add(new MetadataAwareMetadataProvider());
        }
    }
}
