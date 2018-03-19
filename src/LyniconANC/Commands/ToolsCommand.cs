using Lynicon.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Commands
{
    public abstract class ToolsCommandBase
    {
        public abstract string CommandWord { get; }

        public abstract bool Execute(params string[] args);

        public string GetProjectBase()
        {
            var location = Assembly.GetEntryAssembly().Location;
            return location.UpTo(Path.DirectorySeparatorChar.ToString() + "bin");
        }
    }
}
