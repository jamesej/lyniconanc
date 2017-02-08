using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyniconANC.Autotests
{
    public static class TestStartupFiles
    {
        public static List<string> Basic = new List<string>
        {
            "using abc.def;",
            "",
            "namespace TestLynANC",
            "{",
            "// hello",
            "\tpublic class Startup", // POS 5
            "\t{",
            "\t\tpublic Startup(IHostingEnvironment env)", // POS 7
            "\t\t{",
            "\t\t\tvar builder = new ConfigurationBuilder()",
            "\t\t\t\t.AddEnvironmentVariables();",
            "\t\t\tConfiguration = builder.Build();",
            "\t\t}",
            "",
            "\t\tpublic IConfigurationRoot Configuration { get; }",
            "",
            "\t\tpublic void ConfigureServices(IServiceCollection services)", // POS 18
            "\t\t{",
            "\t\t// Add framework services.",
            "\t\tservices.AddMvc();",
            "\t\t}",
            "\t\t",
            "\t\tpublic void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)",
            "\t\t{",
            "\t\t\tloggerFactory.AddConsole(Configuration.GetSection(\"Logging\"));",
            "\t\t\tloggerFactory.AddDebug();",
            "\t\t\t",
            "\t\t\tif (env.IsDevelopment())",
            "\t\t\t{",
            "\t\t\t\tapp.UseDeveloperExceptionPage();",
            "\t\t\t\tapp.UseBrowserLink();",
            "\t\t\t}",
            "\t\t\telse",
            "\t\t\t{",
            "\t\t\t\tapp.UseExceptionHandler(\"/Home/Error\");",
            "\t\t\t}",
            "\t\t\t",
            "\t\t\tapp.UseStaticFiles();",
            "\t\t\t",
            "\t\t\tapp.UseMvc(routes =>",
            "\t\t\t{",
            "\t\t\t\troutes.MapRoute(",
            "\t\t\t\t\tname: \"default\"",
            "\t\t\t\t\ttemplate: \"{controller=Home}/{action=Index}/{id?}\")",
            "\t\t\t});",
            "\t\t}",
            "\t}",
            "}"
        };

        public static List<string> Extended1 = new List<string>
        {
            "using abc.def;",
            "",
            "namespace TestLynANC",
            "{",
            "// hello",
            "\tpublic class Startup", // POS 5
            "\t{",
            "\t\tpublic Startup(IHostingEnvironment env)", // POS 7
            "\t\t{",
            "\t\t\tvar builder = new ConfigurationBuilder()",
            "\t\t\t\t.AddEnvironmentVariables();",
            "\t\t\tConfiguration = builder.Build();",
            "\t\t}",
            "",
            "\t\tpublic IConfigurationRoot Configuration { get; }",
            "",
            "\t\t// Some comment stuff",
            "\t\tpublic void ConfigureServices(IServiceCollection services)", // POS 18
            "\t\t{",
            "\t\t// Add framework services.",
            "\t\tservices.AddMvc();",
            "\t\t",
            "\t\tservices.AddSingleton<IBlah>(new BlahBase());",
            "\t\t}",
            "\t\t",
            "\t\tpublic void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime lt)",
            "\t\t{",
            "\t\t\tloggerFactory.AddConsole(Configuration.GetSection(\"Logging\"));",
            "\t\t\tloggerFactory.AddDebug();",
            "\t\t\t",
            "\t\t\tif (env.IsDevelopment())",
            "\t\t\t{",
            "\t\t\t\tapp.UseDeveloperExceptionPage();",
            "\t\t\t\tapp.UseBrowserLink();",
            "\t\t\t}",
            "\t\t\telse",
            "\t\t\t{",
            "\t\t\t\tapp.UseExceptionHandler(\"/Home/Error\");",
            "\t\t\t}",
            "\t\t\t",
            "\t\t\tapp.UseStaticFiles();",
            "\t\t\t",
            "\t\t\tapp.UseMvc(routes =>",
            "\t\t\t{",
            "\t\t\t\troutes.MapRoute(",
            "\t\t\t\t\tname: \"default\"",
            "\t\t\t\t\ttemplate: \"{controller=Home}/{action=Index}/{id?}\")",
            "\t\t\t});",
            "\t\t}",
            "\t}",
            "}"
        };
    }
}
