using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Models
{
    /// <summary>
    /// Applied to a container which supports basic auditing metadata, enables automatic audit data
    /// setting in the Data API
    /// </summary>
    public interface IBasicAuditable
    {
        /// <summary>
        /// Date created
        /// </summary>
        DateTime Created { get; set; }
        /// <summary>
        /// Id of the user who created it
        /// </summary>
        string UserCreated { get; set; }
        /// <summary>
        /// Date last updated
        /// </summary>
        DateTime Updated { get; set; }
        /// <summary>
        /// Id of the user who last updated it
        /// </summary>
        string UserUpdated { get; set; }
    }
}
