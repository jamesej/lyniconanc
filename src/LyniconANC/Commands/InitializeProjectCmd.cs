using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using Lynicon.Utility;

namespace Lynicon.Commands
{

    public class InitializeProjectCmd : ToolsCommandBase
    {
        public override string CommandWord
        {
            get
            {
                return "initialize-project";
            }
        }
        public override bool Execute(params string[] args)
        {
            var fileModel = new FileModel(Path.Combine(this.GetProjectBase(), "Startup.cs"));
            UpdateStartup(fileModel);
            fileModel.Write();

            fileModel = new FileModel(Path.Combine(this.GetProjectBase(), "appsettings.json"), "///*", true);
            UpdateAppSettings(fileModel);
            fileModel.Write();

            fileModel = new FileModel(Path.Combine(this.GetProjectBase(), "Program.cs"));
            UpdateProgram(fileModel);
            fileModel.Write();

            // Wait until this comes back
            DownloadFilesAsync().Wait();

            return true;
        }

        public async Task DownloadFilesAsync()
        {
            var downloader = new ZipDownloader();
            var version = this.GetType().GetTypeInfo().Assembly.GetName().Version;
            string vsn = string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
            Uri lynSite = new Uri("http://www.lynicon.com", UriKind.Absolute);
            string url = "install/lyniconanc." + vsn + ".zip";
            await downloader.Download(new Uri(lynSite, url), this.GetProjectBase(), true);
        }

        public bool UpdateStartup(FileModel fileModel)
        {
            bool added;
            bool found;
            bool succeeded = true;

            fileModel.ToTop();

            if (fileModel.FindLineContains(".AddLynicon("))
                return true;

            fileModel.ToTop();

            found = fileModel.FindLineContains("namespace");
            if (found)
            {
                fileModel.Jump(-1);
                fileModel.InsertUniqueLineWithIndent("using System.Reflection;");
                fileModel.InsertUniqueLineWithIndent("using Lynicon.Logging;");
                fileModel.InsertUniqueLineWithIndent("using Lynicon.Membership;");
                fileModel.InsertUniqueLineWithIndent("using Lynicon.Modules;");
                fileModel.InsertUniqueLineWithIndent("using Lynicon.Routing;");
                fileModel.InsertUniqueLineWithIndent("using Lynicon.Services;");
                fileModel.InsertUniqueLineWithIndent("using Lynicon.Startup;");
                fileModel.InsertUniqueLineWithIndent("using Microsoft.AspNetCore.Identity.EntityFrameworkCore;");
                fileModel.InsertUniqueLineWithIndent("using Microsoft.AspNetCore.Identity;"); // only needed for core 2
                fileModel.InsertLineWithIndent("");

                Console.WriteLine("Added Lynicon namespaces");
            }
            else
            {
                succeeded = false;
                Console.WriteLine("Failed to add Lynicon namespaces");
            }
            
            // Update constructor

            found = fileModel.FindLineContains("Startup(");

            string envVar = "env";
            if (fileModel.CurrentLine.Contains("IHostingEnvironment"))
                envVar = fileModel.CurrentLine.After("IHostingEnvironment").UpTo(",").UpTo(")").Trim();
            else
            {
                found = fileModel.ReplaceText(")", ", IHostingEnvironment env)");
                if (found)
                    Console.WriteLine("Added IHostingEnvironment parameter to Startup()");
                else
                    Console.WriteLine("Failed to add IHostingEnvironment parameter to Startup()");
            }

            found = found && fileModel.FindEndOfMethod();
            if (found)
            {
                added = fileModel.InsertUniqueLineWithIndent(envVar + ".ConfigureLog4Net(\"log4net.xml\");");
                if (added)
                    Console.WriteLine("Added log4net configuration");
            }
            else
            {
                Console.WriteLine("Failed to add log4net configuration");
                succeeded = false;
            }
                
            // Update ConfigureServices method

            fileModel.ToTop();

            found = fileModel.FindLineContains(".AddMvc(");
            if (found)
            {
                bool doneAddLynOpts = fileModel.ReplaceText(".AddMvc()", ".AddMvc(options => options.AddLyniconOptions())");
                if (!doneAddLynOpts)
                    doneAddLynOpts = fileModel.ReplaceText(".AddMvc(options => options.", ".AddMvc(options => options.AddLyniconOptions()");
                if (!doneAddLynOpts)
                {
                    succeeded = false;
                    Console.WriteLine("Failed to add setup for Lynicon options in MVC");
                }
                else
                    Console.WriteLine("Added Lynicon options in MVC");
                bool addedAppPart = fileModel.InsertTextAfterMatchingBracket(".AddMvc(", ".AddApplicationPart(typeof(LyniconSystem).GetTypeInfo().Assembly)");
                if (!addedAppPart)
                {
                    succeeded = false;
                    Console.WriteLine("Failed to add adding application part for Lynicon");
                }
                else
                    Console.WriteLine("Added adding application part for Lynicon");
            }
            found = found && fileModel.FindEndOfMethod();
            if (found)
            {
                added = fileModel.InsertUniqueLineWithIndent("services.AddIdentity<User, IdentityRole>()");
                if (added)
                {
                    fileModel.InsertLineWithIndent("\t.AddDefaultTokenProviders();");
                    Console.WriteLine("Added services for ASP.Net Identity");
                }
                else
                {
                    Console.WriteLine("The startup file already is initialising ASP.Net Identity - if you can, remove this or if not possible, use LyniconANC.Identity package and adapt it for your existing configuration");
                    return false;
                }

                fileModel.InsertLineWithIndent("");

                if (EnsureCalledAndHasOptionsMethod(fileModel, "services.AddAuthorization(", "AddLyniconAuthorization()"))
                {
                    Console.WriteLine("Added Lynicon Authorization");
                    fileModel.InsertLineWithIndent("");
                }
                else
                    Console.WriteLine("Failed to add Lynicon Authorization");

                fileModel.InsertLineWithIndent("services.AddLynicon(options =>");
                fileModel.InsertLineWithIndent("\toptions.UseConfiguration(Configuration.GetSection(\"Lynicon:Core\"))");
                fileModel.InsertLineWithIndent("\t.UseModule<CoreModule>())");
                fileModel.InsertLineWithIndent(".AddLyniconIdentity();", backIndent: 2);
            }
            else
            {
                succeeded = false;
                Console.WriteLine("Failed to add ASP.Net Identity services setup");
            }

            // Update Configure method

            fileModel.ToTop();

            found = fileModel.FindLineContains("void Configure(");

            if (!found)
            {
                Console.WriteLine("Failed to set up Configure method: can't find Configure method");
                return false;
            }

            string lifeVar = "life";
            if (fileModel.CurrentLine.Contains("IApplicationLifetime"))
                lifeVar = fileModel.CurrentLine.After("IApplicationLifetime").UpTo(",").UpTo(")").Trim();
            else
            {
                found = fileModel.ReplaceText(")", ", IApplicationLifetime life)");
                if (found)
                    Console.WriteLine("Added IApplicationLifetime parameter to Configure()");
                else
                    Console.WriteLine("Failed to add IApplicationLifetime parameter to Configure()");
            }

            int currLine = fileModel.LineNum;
            found = fileModel.FindLineContains(".UseAuthentication()");
            if (!found)
                fileModel.LineNum = currLine;

            if (!fileModel.FindLineContains(".UseMvc("))
            {
                Console.WriteLine("Failed to set up Configure method: no .UseMvc() call");
                return false;
            }

            if (!found)
            {
                fileModel.Jump(-1);
                fileModel.InsertUniqueLineWithIndent("app.UseAuthentication();");
                fileModel.InsertLineWithIndent("");
                Console.WriteLine("Added UseAuthentication()");
            }

            fileModel.InsertUniqueLineWithIndent("app.ConstructLynicon();");
            Console.WriteLine("Added ConstructLynicon()");

            fileModel.FindLineContains(".UseMvc(");
            found = fileModel.ReplaceText(".UseMvc()", ".UseMvc(routes => { routes.MapLyniconRoutes(); })");
            if (!found)
                found = fileModel.ReplaceText(".UseMvc(routes => routes.)", ".UserMvc(routes => routes.MapLyniconRoutes().");
            if (!found)
            {
                found = fileModel.FindLineIs("{");
                fileModel.InsertUniqueLineWithIndent("routes.MapLyniconRoutes();", true);
            }
            if (found)
                Console.WriteLine("Added mapping Lynicon routes");
            else
                Console.WriteLine("Failed to add mapping Lynicon routes");

            found = fileModel.FindPrevLineContains("void Configure(");
            found = found && fileModel.FindEndOfMethod();
            if (found)
            {
                fileModel.InsertLineWithIndent("");
                found = fileModel.InsertUniqueLineWithIndent("app.InitialiseLynicon(" + lifeVar + ");");
            }

            if (found)
                Console.WriteLine("Added initialising Lynicon");
            else
                Console.WriteLine("Failed to add initialising Lynicon");

            return succeeded;
        }

        private bool EnsureCalledAndHasOptionsMethod(FileModel fileModel, string methodLhs, string optionsMethod)
        {
            int currLineNum = fileModel.LineNum;
            bool found = fileModel.FindLineContains(methodLhs);
            if (!found)
            {
                fileModel.ToTop();
                found = fileModel.FindLineContains(methodLhs);
            }
            if (found)
            {
                bool done = fileModel.ReplaceText(methodLhs + ")", methodLhs + "options => options." + optionsMethod + ")");
                if (!done)
                    done = fileModel.ReplaceText(methodLhs + "options => options.", methodLhs + "options => options." + optionsMethod);
                return done;
            }
            else
            {
                fileModel.LineNum = currLineNum;
                fileModel.InsertLineWithIndent(methodLhs + "options => options." + optionsMethod + ");");
            }

            return true;
        }

        public bool UpdateAppSettings(FileModel fileModel)
        {
            bool found;

            found = fileModel.FindLineContains("\"Lynicon\": {");
            if (found)
            {
                Console.WriteLine("App Settings already updated");
                return false;
            }

            fileModel.Jump(9999);
            found = fileModel.FindPrevLineIs("}");
            if (found)
            {
                fileModel.Jump(-1);
                fileModel.ReplaceText("}", "},");
                fileModel.InsertUniqueLineWithIndent("\"Lynicon\": {", backIndent: 2);
                fileModel.InsertUniqueLineWithIndent("  \"Core\": {");
                fileModel.InsertUniqueLineWithIndent("  \"FileManagerRoot\": \"/Uploads/\",");
                fileModel.InsertUniqueLineWithIndent("\"SqlConnectionString\": \"...\",");
                fileModel.InsertUniqueLineWithIndent("\"LyniconAreaBaseUrl\": \"/Areas/Lynicon/\"");
                fileModel.InsertLineWithIndent("}", backIndent: 2);
                fileModel.InsertLineWithIndent("}", backIndent: 4);

                Console.WriteLine("Added basic Lynicon configuration");
            }
            else
            {
                Console.WriteLine("Failed to add basic Lynicon configuration");
                return false;
            }


            return true;
        }

        public bool UpdateProgram(FileModel fileModel)
        {
            bool found;

            fileModel.ToTop();

            found = fileModel.FindLineContains("namespace");
            if (found)
            {
                fileModel.Jump(-1);
                fileModel.InsertUniqueLineWithIndent("using Lynicon.Commands;");
                fileModel.InsertLineWithIndent("");

                Console.WriteLine("Added using to Program.cs");
            }
            else
            {
                Console.WriteLine("Failed using to Program.cs");
                return false;
            }

            found = fileModel.FindLineContains("UseContentRoot(");
            if (found && !fileModel.LineContains("ContentRootLocator.GetContentRoot(args)"))
            {
                fileModel.ReplaceText("UseContentRoot(", "UseContentRoot(ContentRootLocator.GetContentRoot(args) ?? ");
                Console.WriteLine("Added content route redirection to Program.cs");
            }
            else
            {
                fileModel.ToTop();
                found = fileModel.FindLineContains(".UseStartup");
                if (found)
                {
                    fileModel.InsertUniqueLineWithIndent(".UseContentRoot(ContentRootLocator.GetContentRoot(args) ?? Directory.GetCurrentDirectory())");
                }

                if (!found)
                {
                    Console.WriteLine("Failed to add content route redirection to Program.cs");
                    return false;
                }
            }

            return true;
        }
    }
}
