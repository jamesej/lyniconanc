using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lynicon.Exceptions
{
    public class ProhibitedActionException : Exception
    {
        public ProhibitedActionException() : base()
        { }
        public ProhibitedActionException(string message) : base(message)
        { }
        public ProhibitedActionException(string message, Exception innerEx) : base(message, innerEx)
        { }
    }
}
