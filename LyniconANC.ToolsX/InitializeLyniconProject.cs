using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Diagnostics;
using EnvDTE;
using System.Reflection;
using System.IO;

namespace Lynicon.Tools
{
    // Windows PowerShell assembly.

    // Declare the class as a cmdlet and specify and 
    // appropriate verb and noun for the cmdlet name.
    [Cmdlet(VerbsData.Initialize, "LyniconProject")]
    public class InitializeLyniconProjectCommand : Cmdlet
    {
        protected override void ProcessRecord()
        {
            var fileModel = ProjectContextLoader.GetItemFileModel(SendMessage, "Global.asax");

            bool found = fileModel.FindLineContains("Application_Start()")
                         && fileModel.FindLineIs("{");
            if (found)
            {
                fileModel.InsertUniqueLineWithIndent("// Lynicon install inserted these 2 lines", useIndentAfter: true);
                fileModel.InsertUniqueLineWithIndent("LyniconConfig.RegisterModules();", useIndentAfter: true);
                fileModel.InsertUniqueLineWithIndent("LyniconConfig.InitialiseDataApi();", useIndentAfter: true);
                fileModel.InsertLineWithIndent("");

                found = fileModel.FindLineContains("RouteConfig.RegisterRoutes(");
                if (fileModel.FindLineContains("BundleConfig.RegisterBundles("))
                    found = true;
                if (found)
                {
                    fileModel.InsertLineWithIndent("");
                    fileModel.InsertUniqueLineWithIndent("// Lynicon install inserted this line");
                    fileModel.InsertUniqueLineWithIndent("LyniconConfig.Initialise();");
                    fileModel.InsertLineWithIndent("");
                }
            }
            fileModel.Write();

            var filtersFile = ProjectContextLoader.GetItemFileModel(SendMessage, "App_Start/FilterConfig.cs");

            found = filtersFile.FindLineContains("using System.Web.Mvc;");
            if (found)
            {
                filtersFile.InsertUniqueLineWithIndent("using Lynicon.Attributes;");
            }
            
            found = filtersFile.FindLineContains("public static void RegisterGlobalFilters(")
                    && filtersFile.FindLineIs("{");
            if (found)
            {
                filtersFile.InsertLineWithIndent("// Lynicon install inserted these 2 lines", useIndentAfter: true);
                filtersFile.InsertLineWithIndent("filters.Add(new ProcessIncludesAttribute());", useIndentAfter: true);
                filtersFile.InsertLineWithIndent("filters.Add(new ProcessHtmlAttribute());", useIndentAfter: true);
            }
            filtersFile.Write();

            WriteObject("Updated Global.asax.cs, FilterConfig.cs");
        }

        public void SendMessage(MessageEventArgs e)
        {
            MessageHandler.Handle(this, e);
        }
    }
}
