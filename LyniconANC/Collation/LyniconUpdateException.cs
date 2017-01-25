using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Collation
{
    /// <summary>
    /// Represent a predictable exception which stops the writing of a content item.  This kind of
    /// exception is intended to be handled and shown to the user as an error, it does not imply
    /// a code defect.
    /// </summary>
    public class LyniconUpdateException : Exception
    {
        public LyniconUpdateException(string msg) : base(msg)
        { }
        public LyniconUpdateException(string msg, Exception inner) : base(msg, inner)
        { }
    }
}
