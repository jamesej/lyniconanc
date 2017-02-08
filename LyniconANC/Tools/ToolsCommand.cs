using Lynicon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Tools
{
    public abstract class ToolsCommandBase
    {
        public abstract string CommandWord { get; }

        public abstract bool Execute(params string[] args);

        public string GetProjectBase()
        {
            var location = Assembly.GetEntryAssembly().Location;
            return location.UpTo("\\bin");
        }
    }
}
