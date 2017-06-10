using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Lynicon.Routing;
using Lynicon.Test.Models;
using Lynicon.Startup;
using Lynicon.Modules;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Lynicon.Membership;
using Microsoft.AspNetCore.Identity;
using Lynicon.Services;
using Lynicon.Extensibility;
using LyniconANC.Release.Models;
using Lynicon.Collation;
using Lynicon.Repositories;
using Lynicon.DataSources;
using Lynicon.Editors;
using System.Reflection;
using Lynicon.Logging;

namespace LyniconANC.Release
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();

            env.ConfigureLog4Net("log4net.xml");
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            //services.AddDbContext<ApplicationDbContext>(options =>
            //    options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            // Add framework services.
            services.AddMvc(options => options.AddLyniconOptions())
                .AddApplicationPart(typeof(LyniconSystem).GetTypeInfo().Assembly);

            services.AddIdentity<User, IdentityRole>()
                //.AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddAuthorization(options =>
                options.AddLyniconAuthorization());

            services.AddLynicon(options =>
                options.UseConfiguration(Configuration.GetSection("Lynicon:Core"))
                    .UseModule<CoreModule>()
                    .UseModule<ContentSchemaModule>()
                    .UseTypeSetup(tsr =>
                        {
                            tsr.SetupType(typeof(TestData), new BasicCollator(tsr.System), new BasicRepository(tsr.System, new CoreDataSourceFactory(tsr.System)), DataDiverter.Instance.NullDivert);
                        }
                    ))
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
                app.UseDatabaseErrorPage();
                //app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseIdentity();

            // Add external authentication middleware below. To configure them please see http://go.microsoft.com/fwlink/?LinkID=532715

            app.ConstructLynicon();

            app.UseMvc(routes =>
            {
                routes.MapLyniconRoutes();
                routes.MapDataRoute<TestContent>("test", "test/{_0}", new { controller = "Test", action = "Index" });
                routes.MapDataRoute<HeaderContent>("header", "test/{_0}", new { controller = "Test", action = "Header" });
                routes.MapDataRoute<List<TestContent>>("test-list", "test-list", new { controller = "Test", action = "List" }, null, new { view = "LyniconListDetail", listView = "ObjectList", rowFields = "Title, TestText" });
                routes.MapDataRoute<TestData>("testData", "testdata/{_0}", new { controller = "Test", action = "Data" }, null, null, DataDiverter.Instance.NullDivert);
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            app.InitialiseLynicon(life);
        }
    }
}
