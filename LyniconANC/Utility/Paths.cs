using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Utility
{
    /// <summary>
    /// Utility methods for finding solution paths
    /// </summary>
    public static class Paths
    {
        private static string appDataRelativePath = null;
        /// <summary>
        /// Get the full file path to App_Data relative to the CurrentDomain.BaseDirectory
        /// </summary>
        public static string AppDataRelativePath
        {
            get
            {
                if (appDataRelativePath == null)
                {
                    if (AppDomain.CurrentDomain.BaseDirectory.LastAfter("\\").ToLower() == "bin")
                        appDataRelativePath = "..\\App_Data";
                    else if (AppDomain.CurrentDomain.BaseDirectory.UpToLast("\\").LastAfter("\\") == "bin")
                        appDataRelativePath = "..\\..\\App_Data";
                    else
                        appDataRelativePath = "App_Data";
                }
                return appDataRelativePath;
            }
            set
            {
                appDataRelativePath = value;
            }
        }

        /// <summary>
        /// Get the full file path to App_Data
        /// </summary>
        public static string AppDataPath
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDataRelativePath);
            }
        }
    }
}
