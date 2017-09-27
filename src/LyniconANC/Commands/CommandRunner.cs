using Lynicon.Services;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Commands
{
    public class CommandRunner : ICommandRunner
    {
        public static bool InterceptAndRunCommands(IServiceProvider services, string[] args)
        {
            var commandRunner = (ICommandRunner)services.GetService(typeof(ICommandRunner));
            if (commandRunner == null)
                commandRunner = new CommandRunner(null);
            return commandRunner.InterceptAndRunCommands(args);
        }

        protected Dictionary<string, ToolsCommandBase> commands = new Dictionary<string, ToolsCommandBase>();

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
            if (args.Length < 1)
                return false;

            var lynArgs = args.SkipWhile(a => a != "--lynicon").ToArray();

            if (lynArgs.Length < 2)
                return false;

            if (!commands.ContainsKey(lynArgs[1].ToLower()))
                return false;

            var cmd = commands[lynArgs[1].ToLower()];

            cmd.Execute(lynArgs.Skip(2).ToArray());

            return true;
        }
    }
}
