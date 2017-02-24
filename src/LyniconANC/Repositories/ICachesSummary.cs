using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Repositories
{
    /// <summary>
    /// Interface for a container which caches its summary
    /// </summary>
    public interface ICachesSummary
    {
        void InvalidateSummary();
        void EnsureSummaryCache();
    }
}
