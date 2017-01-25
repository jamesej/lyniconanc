using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Tools
{
    /// <summary>
    /// Type of message to show
    /// </summary>
    [Serializable]
    public enum MessageType
    {
        Verbose,
        Progress,
        Error,
        Output
    }

    /// <summary>
    /// A message to be shown by a powershell command as an EventArg
    /// </summary>
    [Serializable]
    public class MessageEventArgs : EventArgs
    {
        public string Message { get; set; }

        public string Activity { get; set; }

        public MessageType MessageType { get; set; }

        public Exception Exception { get; set; }

        public MessageEventArgs(string msg)
        {
            MessageType = MessageType.Verbose;
            Message = msg;
        }
        public MessageEventArgs(MessageType type, string msg)
        {
            MessageType = type;
            Message = msg;
        }
        public MessageEventArgs(string activity, string msg)
        {
            Activity = activity;
            Message = msg;
            MessageType = MessageType.Progress;
        }
        public MessageEventArgs(Exception ex)
        {
            MessageType = MessageType.Error;
            Exception = ex;
        }

        public void MarshalExceptionToBase()
        {
            if (this.Exception != null && this.Exception.GetType() != typeof(Exception))
            {
                this.Exception = new Exception(this.Exception.ToString());
            }
        }
    }
}
