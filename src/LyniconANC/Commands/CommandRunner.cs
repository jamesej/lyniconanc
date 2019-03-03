using Lynicon.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Commands
{
    public class CommandRunner : ICommandRunner
    {
        public class InterceptRunStartupFilter : IStartupFilter
        {
            string[] args;

            public InterceptRunStartupFilter(string[] args)
            {
                this.args = args;
            }

            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return builder =>
                {
                    next(builder);
                    if (InterceptAndRunCommands(builder.ApplicationServices, args))
                    {
                        // terminate the process, to avoid starting the web host as we have executed a command
                        System.Diagnostics.Process.GetCurrentProcess().Kill();
                    }
                };
            }
        }

        public static bool InterceptAndRunCommands(IServiceProvider services, string[] args)
        {
            return GetCommandRunner(services).InterceptAndRunCommands(args);
        }

        private static ICommandRunner GetCommandRunner(IServiceProvider services)
        {
            var commandRunner = (ICommandRunner)services.GetService(typeof(ICommandRunner));
            if (commandRunner == null)
                commandRunner = new CommandRunner(null);
            return commandRunner;
        }

        /// <summary>
        /// Runs at the end of services setup
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="args"></param>
        public static IWebHostBuilder SetupCommandInterception(IWebHostBuilder builder, string[] args)
        {
            builder.ConfigureServices(services =>
            {
                if (new CommandRunner(null).WillRunCommand(args))
                {
                    builder.CaptureStartupErrors(false);
                    var mi = typeof(HostingAbstractionsWebHostBuilderExtensions).GetMethod("SuppressStatusMessages", BindingFlags.Public | BindingFlags.Static);
                    if (mi != null)
                        mi.Invoke(null, new object[] { builder, true });
                    services.Add(ServiceDescriptor.Singleton(typeof(IStartupFilter), new InterceptRunStartupFilter(args)));
                }
            });

            return builder;
        }

        protected Dictionary<string, ToolsCommandBase> commands = new Dictionary<string, ToolsCommandBase>();

        /// <summary>
        /// Initialize a command runner
        /// </summary>
        /// <param name="sys">LyniconSystem which will be used by commands. Can be null if only want to check WillRunCommand</param>
        public CommandRunner(LyniconSystem sys)
        {
            this.RegisterCommand(new InitializeProjectCmd());
            this.RegisterCommand(new InitializeDatabaseCmd(sys));
            this.RegisterCommand(new InitializeAdminCmd(sys));
        }

        public bool RegisterCommand(ToolsCommandBase toolsCommand)
        {
            if (commands.ContainsKey(toolsCommand.CommandWord))
                return false;

            commands.Add(toolsCommand.CommandWord, toolsCommand);
            return true;
        }

        public bool InterceptAndRunCommands(string[] args)
        {
            var runCmd = GetCommand(args);
            if (runCmd == null)
                return false;

            runCmd();

            return true;
        }

        private Action GetCommand(string[] args)
        {
            var lynArgs = GetLynArgs(args);

            if (lynArgs == null)
                return null;

            if (!commands.ContainsKey(lynArgs[1].ToLower()))
                return null;

            var cmd = commands[lynArgs[1].ToLower()];

            return () => cmd.Execute(lynArgs.Skip(2).ToArray());
        }

        private string[] GetLynArgs(string[] args)
        {
            if (args.Length < 1)
                return null;

            var lynArgs = args.SkipWhile(a => a != "--lynicon").ToArray();

            if (lynArgs.Length < 2)
                return null;

            return lynArgs;
        }

        public bool WillRunCommand(string[] args)
        {
            // Don't check for existence of the actual command as we won't have registered commands
            // at this point
            return GetLynArgs(args) != null;
        }
    }
}
