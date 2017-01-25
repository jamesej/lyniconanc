using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Tools
{
    /// <summary>
    /// Base class for creating a MarshalByRefObject in a new AppDomain and receiving messages from it for the purpose
    /// of loading the executable output of a Visual Studio project to perform a task
    /// </summary>
    /// <typeparam name="T">The concrete class of the MarshalByRefObject to be created in the new AppDomain to perform operations there</typeparam>
    [Serializable]
    public class LocalProjectContextCommand<T> : MarshalByRefObject where T : RemoteProjectContextCommand
    {
        protected Cmdlet caller;
        protected T remote;

        public LocalProjectContextCommand(Cmdlet caller)
        {
            this.caller = caller;
        }

        /// <summary>
        /// Create the new app domain and create a remote proxy (type T) to do work there
        /// </summary>
        /// <param name="appDomainName"></param>
        protected void InitializeNewAppDomain(AppDomain appDomain)
        {
            remote = (T)appDomain.CreateInstanceFromAndUnwrap(typeof(T).Assembly.Location, typeof(T).FullName);

            remote.InitializeRemoteAssemblyResolution(this.GetType().Assembly.Location);
            remote.OnMessage += HandleMessage;
        }

        /// <summary>
        /// Handle event raised by remote proxy in order to communicate back a message
        /// </summary>
        /// <param name="sender">the event raiser</param>
        /// <param name="e">the event containing the sent message</param>
        public void HandleMessage(object sender, MessageEventArgs e)
        {
            Handle(e);
        }
        public void Handle(MessageEventArgs e)
        {
            MessageHandler.Handle(this.caller, e);
        }
    }
}
