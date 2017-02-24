using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LyniconANC.Exceptions
{
    public class ApplicationException : Exception
    {
        public ApplicationException() : base()
        { }
        public ApplicationException(string message) : base(message)
        { }
        public ApplicationException(string message, Exception innerEx) : base(message, innerEx)
        { }
    }
}
