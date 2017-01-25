using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;

namespace Lynicon.Tools
{
    public static class DTEFinder
    {
        /// <summary>
        /// Get the object model for the currently running instance of Visual Studio
        /// </summary>
        /// <returns>COM object model for Visual Studio</returns>
        public static EnvDTE80.DTE2 GetDTE()
        {
            EnvDTE80.DTE2 dte2;
            try
            {
                dte2 = (EnvDTE80.DTE2)Marshal.GetActiveObject("VisualStudio.DTE.12.0");
            }
            catch
            {
                try
                {
                    dte2 = (EnvDTE80.DTE2)Marshal.GetActiveObject("VisualStudio.DTE.14.0");
                }
                catch (Exception ex)
                {
                    throw new Exception("Can't find running Visual Studio instance", ex);
                }
            }
            return dte2;
        }
    }
}
