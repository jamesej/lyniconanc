using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Tools
{
    public static class MessageHandler
    {
        /// <summary>
        /// Output a message in the appropriate way in a powershell command
        /// </summary>
        /// <param name="caller">the Cmdlet object running</param>
        /// <param name="e">the event containing the message</param>
        public static void Handle(Cmdlet caller, MessageEventArgs e)
        {
            switch (e.MessageType)
            {
                case MessageType.Verbose:
                    caller.WriteVerbose(e.Message);
                    break;
                case MessageType.Progress:
                    caller.WriteProgress(new ProgressRecord(0, e.Activity, e.Message));
                    break;
                case MessageType.Error:
                    ToolsHelper.WriteException(caller, e.Exception);
                    caller.ThrowTerminatingError(new ErrorRecord(e.Exception, "0", ErrorCategory.InvalidOperation, caller));
                    break;
                case MessageType.Output:
                    caller.WriteObject(e.Message);
                    break;
            }
        }
    }
}
