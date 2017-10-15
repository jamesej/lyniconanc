using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System.Reflection;
using Lynicon.Logging;
using Lynicon.Membership;
using Lynicon.Modules;
using Lynicon.Routing;
using Lynicon.Services;
using Lynicon.Startup;
using Microsoft.AspNetCore.Identity;
using LyniconANC.Release.Models;

namespace LyniconANC.Release
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
            env.ConfigureLog4Net("log4net.xml");
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc(options => options.AddLyniconOptions()).AddApplicationPart(typeof(LyniconSystem).GetTypeInfo().Assembly);
            services.AddIdentity<User, IdentityRole>()
            	.AddDefaultTokenProviders();
            	
            	services.AddAuthorization(options => options.AddLyniconAuthorization());
            	
            	services.AddLynicon(options =>
            		options.UseConfiguration(Configuration.GetSection("Lynicon:Core"))
            			.UseModule<CoreModule>()
						.UseModule<ContentSchemaModule>())
            	.AddLyniconIdentity();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime life)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

app.UseAuthentication();

app.ConstructLynicon();
            app.UseMvc(routes =>
            {
                routes.MapLyniconRoutes();
                routes.MapDataRoute<HomeContent>("home", "", new { controller = "Home", action = "Index" });
                routes.MapDataRoute<CommonContent>("common", "lynicon/common", new { controller = "Common", action = "Common" });
                routes.MapDataRoute<TileContent>("tiles", "tiles/{_0}", new { controller = "Tile", action = "Tile" });
                routes.MapDataRoute<EquipmentContent>("equipment", "equipment/{_0}", new { controller = "Equipment", action = "Equipment" });
                routes.MapDataRoute<MaterialsLandingContent>("material-landing", "materials", new { controller = "Tile", action = "MaterialsLanding" });
                routes.MapDataRoute<TileMaterialContent>("materials", "materials/{_0}", new { controller = "Tile", action = "TileMaterial" });
                routes.MapDataRoute<List<TileContent>>("tiles-api", "api/tiles", new { controller = "Api", action = "Tiles" });
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
            
            app.InitialiseLynicon(life);
        }
    }
}
