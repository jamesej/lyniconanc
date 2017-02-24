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
        IDataSource Create(bool forSummaries);
    }
}
