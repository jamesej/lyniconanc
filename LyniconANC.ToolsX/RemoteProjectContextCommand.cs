using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Tools
{
    /// <summary>
    /// Base class for a remote proxy to perform operations in another AppDomain having loaded the executable
    /// output of the current Visual Studio project
    /// </summary>
    [Serializable]
    public class RemoteProjectContextCommand : MarshalByRefObject
    {
        public event EventHandler<MessageEventArgs> OnMessage;

        /// <summary>
        /// Raise an event to pass a message back to the parent AppDomain
        /// </summary>
        /// <param name="e">The message in an EventArgs</param>
        public void Message(MessageEventArgs e)
        {
            if (OnMessage != null)
            {
                e.MarshalExceptionToBase();
                OnMessage(this, e);
            }
        }

        /// <summary>
        /// Set up assembly resolution for the remote AppDomain allowing it to resolve the current
        /// assembly as well as those from the Visual Studio project whose output it is loading
        /// </summary>
        /// <param name="toolsAssemblyPath"></param>
        public void InitializeRemoteAssemblyResolution(string toolsAssemblyPath)
        {
            ProjectContextLoader.ToolsAssemblyPath = toolsAssemblyPath;
            ProjectContextLoader.SetupAssemblyResolution();
        }
    }
}
