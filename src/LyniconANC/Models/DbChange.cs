using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Models
{
    /// <summary>
    /// Database record recording a change to the database schema
    /// </summary>
    public class DbChange
    {
        /// <summary>
        /// Id of the change
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Description/name of the change
        /// </summary>
        public string Change { get; set; }
        /// <summary>
        /// The date when the change was made
        /// </summary>
        public DateTime ChangedWhen { get; set; }
    }
}
