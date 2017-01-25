using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Collation
{
    /// <summary>
    /// Functions for processing address paths
    /// </summary>
    public static class PathFunctions
    {
        /// <summary>
        /// Transform an address path using a C# string format pattern
        /// </summary>
        /// <param name="path">Ampersand-separated address path</param>
        /// <param name="redirectPattern">C# string format with '{0}', '{1}' etc representing the ampersand-separated elements of the input address path</param>
        /// <returns></returns>
        public static string Redirect(string path, string redirectPattern)
        {
            return string.Format(redirectPattern, path.Split('&').Cast<object>().ToArray());
        }
    }
}
