using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;


namespace Lynicon.Tools
{
    public class ProjectContextLoader
    {
        public static string ToolsAssemblyPath { get; set; }

        public static EnvDTE80.DTE2 DTE2 { get; set; }
        private static string DllPath { get; set; }

        public static EnvDTE.Project MainProject { get; set; }

        public static string AssName { get; set; }

        public static string AssPath { get; set; }

        public static string DefaultNs { get; set; }

        public static string RootPath { get; set; }

        public static string WebConfigPath { get; set; }

        public static Assembly StartupAssembly { get; set; }

        private static bool ContextLoaded { get; set; }

        private static bool DataApiInitialised { get; set; }

        static ProjectContextLoader()
        {
            DTE2 = null;
            ContextLoaded = false;
            DataApiInitialised = false;
        }

        /// <summary>
        /// Load up information for the currently running Visual Studio object
        /// </summary>
        /// <param name="sendMessage">method to send messages for display</param>
        public static void EnsureDTE(Action<MessageEventArgs> sendMessage)
        {
            if (DTE2 != null)
                return;

            DTE2 = DTEFinder.GetDTE();
            //var mvcProj = dte.Solution.Projects.Cast<Project>().FirstOrDefault(p => p.Kind == Constants.vsProjectKind)
            string startupProjIdx = ((Array)DTE2.Solution.SolutionBuild.StartupProjects).Cast<string>().First();
            var startupProj = DTE2.Solution.Item(startupProjIdx);
            if (sendMessage != null)
                sendMessage(new MessageEventArgs("Found startup project: " + startupProj.Name));

            MainProject = startupProj;
            AssName = startupProj.Properties.Item("AssemblyName").Value.ToString();
            DefaultNs = startupProj.Properties.Item("DefaultNamespace").Value.ToString();
            RootPath = startupProj.FullName.UpToLast("\\");
            AssPath = RootPath + "\\bin\\" + AssName + ".dll";
            WebConfigPath = RootPath + "\\web.config";
        }

        public static void SetupAssemblyResolution()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveRuntimeRedirects;
        }

        /// <summary>
        /// Set up environment parameters and load assemblies from the output of the currently running
        /// Visual Studio project
        /// </summary>
        /// <param name="sendMessage"></param>
        public static void EnsureLoaded(Action<MessageEventArgs> sendMessage)
        {
            if (ContextLoaded)
                return;

            EnsureDTE(sendMessage);

            if (sendMessage != null)
                sendMessage(new MessageEventArgs("Initializing Project Context", "Loading assembly: " + AssPath));

            System.AppDomain.CurrentDomain.SetData("APPBASE", RootPath);
            System.AppDomain.CurrentDomain.SetData("PRIVATE_BINPATH", "bin");
            System.AppDomain.CurrentDomain.SetData("DataDirectory", RootPath + "\\App_Data");

            DllPath = RootPath + "\\bin\\";

            // Use the web config of the website project as config for this appdomain
            using (AppConfig.Change(WebConfigPath))
            {
                StartupAssembly = null;
                try
                {
                    StartupAssembly = Assembly.LoadFrom(AssPath);
                    var x = StartupAssembly.GetType("log4net.Config.Log4NetConfigurationSectionHandler");
                }
                catch (Exception ex)
                {
                    if (sendMessage != null)
                    {
                        if (ex is FileNotFoundException && ex.Message.StartsWith("Could not load"))
                            sendMessage(new MessageEventArgs(new Exception("Couldn't load the project assembly: you may not have compiled it", ex)));
                        else
                            sendMessage(new MessageEventArgs(ex));
                    }
                }

                Assembly efSqlAss = null;
                try
                {
                    // Load this manually as it will generally not be referenced but is referenced by Lynicon
                    efSqlAss = Assembly.LoadFrom(RootPath + "\\bin\\EntityFramework.SqlServer.dll");
                    var x = efSqlAss.GetType("System.Data.Entity.SqlServer.SqlProviderServices");
                }
                catch { }

                if (sendMessage != null)
                    sendMessage(new MessageEventArgs("Initializing Project Context", "Assembly loaded"));
            }

            ContextLoaded = true;
        }

        /// <summary>
        /// Load up Lynicon modules and initialise the Lynicon data API after having loaded outputs from the current Visual Studio project
        /// which references Lynicon
        /// </summary>
        /// <param name="sendMessage"></param>
        public static void InitialiseDataApi(Action<MessageEventArgs> sendMessage)
        {
            if (DataApiInitialised)
                return;

            EnsureLoaded(sendMessage);

            using (AppConfig.Change(WebConfigPath))
            {
                string lyniconConfigName = DefaultNs + ".LyniconConfig";

                if (sendMessage != null)
                    sendMessage(new MessageEventArgs("Initializing Project Context", "Trying to load type: " + lyniconConfigName));
                Type lyniconConfig = StartupAssembly.GetType(lyniconConfigName);
                if (lyniconConfig == null)
                    sendMessage(new MessageEventArgs(new Exception("Cannot load type LyniconConfig: you may not have built the solution after installing the Lynicon package")));


                if (sendMessage != null)
                    sendMessage(new MessageEventArgs("Initializing Project Context", "Type loaded"));

                var registerModulesMethod = lyniconConfig.GetMethod("RegisterModules", BindingFlags.Static | BindingFlags.Public);
                var initialiseDataApiMethod = lyniconConfig.GetMethod("InitialiseDataApi", BindingFlags.Static | BindingFlags.Public);
                if ((registerModulesMethod == null || initialiseDataApiMethod == null) && sendMessage != null)
                    sendMessage(new MessageEventArgs(new Exception("Cannot find 'RegisterModules' and 'InitialiseDataApi' methods on LyniconConfig")));

                if (sendMessage != null)
                    sendMessage(new MessageEventArgs("Initializing Project Context", "Initializing Data Api"));
                try
                {
                    registerModulesMethod.Invoke(null, new object[0]);
                    initialiseDataApiMethod.Invoke(null, new object[0]);
                }
                catch (Exception ex)
                {
                    if (sendMessage != null)
                    {
                        sendMessage(new MessageEventArgs(ex));
                    }

                }

                if (sendMessage != null)
                    sendMessage(new MessageEventArgs("Initializing Project Context", "Lynicon initialized"));
            }

            DataApiInitialised = true;
        }

        /// <summary>
        /// Ensures assemblies being loaded can be resolved
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Assembly ResolveRuntimeRedirects(object sender, ResolveEventArgs args)
        {
            try
            {
                // Ensure we can load the assembly containing this code from wherever it is located (which will be outside
                // the path used for the VS project)
                string assName = args.Name.UpTo(",");
                if (assName == "Lynicon.Tools")
                    return Assembly.LoadFrom(ToolsAssemblyPath);

                // This will load any available version of a requested dll found in the DllPath.
                string path = DllPath + assName + ".dll";
                if (File.Exists(path))
                {
                    var assembly = Assembly.LoadFrom(DllPath + assName + ".dll");
                    return assembly;
                }
                else
                    return null;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            return null;
        }

        /// <summary>
        /// Gets an object for manipulating the file behind a project item reference
        /// </summary>
        /// <param name="sendMessage"></param>
        /// <param name="itemName"></param>
        /// <returns></returns>
        public static FileModel GetItemFileModel(Action<MessageEventArgs> sendMessage, string itemName)
        {
            EnsureDTE(sendMessage);

            var global = FindItemByPath(MainProject.ProjectItems, itemName);
            if (global == null)
                sendMessage(new MessageEventArgs(new Exception("Can't find " + itemName + " to update")));
            var globalFileName = global.get_FileNames(1);
            if (!globalFileName.EndsWith(".cs"))
                globalFileName += ".cs";

            var fileModel = new FileModel(globalFileName);
            return fileModel;
        }

        /// <summary>
        /// Find a project item using a path-like address in the project folder hierarchy
        /// </summary>
        /// <param name="topItems">Top level items in the project</param>
        /// <param name="itemPath">path to the required project item</param>
        /// <returns>the project item</returns>
        public static ProjectItem FindItemByPath(ProjectItems topItems, string itemPath)
        {
            ProjectItem item = topItems.OfType<ProjectItem>().FirstOrDefault(pi => pi.Name == itemPath.UpTo("/"));
            if (!itemPath.Contains("/"))
                return item;
            else
                return FindItemByPath(item.ProjectItems, itemPath.After("/"));
        }
    }
}
