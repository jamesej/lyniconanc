using Lynicon.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Tools
{
    public static class ContentRootLocator
    {
        public static string GetContentRoot(string[] args)
        {
            if (args.Length == 0 || args[0].ToLower() != "lynicon")
                return null;

            string curr = Directory.GetCurrentDirectory();
            if (curr.Contains("\\bin\\"))
                return curr.UpTo("\\bin\\");
            else
                return curr;
        }
    }
}
