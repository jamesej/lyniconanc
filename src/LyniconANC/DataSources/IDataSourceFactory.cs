using Lynicon.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.DataSources
{
    /// <summary>
    /// An IDataSourceFactory should not connect to any data source in its constructor.
    /// </summary>
    public interface IDataSourceFactory
    {
        string DataSourceSpecifier { get; }
        LyniconSystem System { get; set; }
        IDataSource Create(bool forSummaries);
    }
}
