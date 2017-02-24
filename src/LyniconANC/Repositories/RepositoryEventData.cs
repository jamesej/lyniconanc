using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Repositories
{
    /// <summary>
    /// Data for a Repository event where the data item is/are changed
    /// </summary>
    public class RepositoryEventData
    {
        /// <summary>
        /// The container being changed
        /// </summary>
        public object Container { get; set; }
        /// <summary>
        /// Whether to bypass normal checks for a user-originated request
        /// </summary>
        public bool BypassChecks { get; set; }
        /// <summary>
        /// Whether the operation was done by one of the event processors, so
        /// the calling code doesn't need to do it
        /// </summary>
        public bool WasHandled { get; set; }

        /// <summary>
        /// Create a new RepositoryEventData
        /// </summary>
        public RepositoryEventData()
        {
            WasHandled = false;
        }
        /// <summary>
        /// Create a new RepositoryEventData with the container and bypass checks flag
        /// </summary>
        /// <param name="container">The container which is being changed</param>
        /// <param name="bypassChecks">Whether to bypass normal validation checks for a user-originated request</param>
        public RepositoryEventData(object container, bool bypassChecks) : this()
        {
            Container = container;
            BypassChecks = bypassChecks;
        }
    }
}
