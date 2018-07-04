using System;
using System.Collections.Generic;
using System.Text;

namespace Lynicon.Commands
{
    public class NonHostingStartupException : Exception
    {
        public NonHostingStartupException(string message) : base(message)
        { }
    }
}
