using Lynicon.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.DataSources
{
    public interface IDataSourceFactory
    {
        string DataSourceSpecifier { get; }
        LyniconSystem System { get; set; }
        IDataSource Create(bool forSummaries);
    }
}
