namespace Lynicon.Tools
{
    public interface ICommandRunner
    {
        bool InterceptAndRunCommands(string[] args);

        bool RegisterCommand(ToolsCommandBase toolsCommand);
    }
}