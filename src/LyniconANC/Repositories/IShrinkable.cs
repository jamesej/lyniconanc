using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Repositories
{
    /// <summary>
    /// Interface to indicate the container has a method to reduce its memory space requirement
    /// </summary>
    public interface IShrinkable
    {
        /// <summary>
        /// Reduce the memory requirement without losing information
        /// </summary>
        void Shrink();
    }
}
