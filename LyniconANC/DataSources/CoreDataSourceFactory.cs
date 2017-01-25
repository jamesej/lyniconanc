using Lynicon.Extensibility;
using Lynicon.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.DataSources
{
    public class CoreDataSourceFactory : IDataSourceFactory
    {
        public string DataSourceSpecifier { get; set; }

        public int? QueryTimeoutSecs { get; set; }

        public IDataSource Create(bool forSummaries)
        {
            return new CoreDataSource(ContextLifetimeMode, forSummaries);
        }

        ContextLifetimeMode contextLifetimeMode = ContextLifetimeMode.PerCall;
        /// <summary>
        /// Set how long the context persists for.  Can be per call to the repository or per request
        /// </summary>
        public ContextLifetimeMode ContextLifetimeMode
        {
            get
            {
                if (RequestContextManager.Instance.CurrentContext == null)    // nowhere to store context if not in a request thread
                    return ContextLifetimeMode.PerCall;
                else
                    return contextLifetimeMode;
            }
            set
            {
                contextLifetimeMode = value;
            }
        }
    }
}
