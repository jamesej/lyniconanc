using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Tools
{
    internal static class ToolsHelper
    {
        /// <summary>
        /// Write out an exception to the Verbose output channel
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="ex"></param>
        public static void WriteException(Cmdlet caller, Exception ex)
        {
            if (ex.InnerException != null)
                WriteException(caller, ex.InnerException);
            caller.WriteVerbose(string.Format("Exception was: {0} at: {1}", ex.Message, ex.StackTrace));
        }
    }
}
