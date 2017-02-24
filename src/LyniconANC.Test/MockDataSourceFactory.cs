using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.DataSources;

namespace LyniconANC.Test
{
    public class MockDataSourceFactory : IDataSourceFactory
    {
        #region IDataSourceFactory Members

        public string DataSourceSpecifier
        {
            get { return ""; }
        }

        public IDataSource Create(bool forSummaries)
        {
            return new MockDataSource();
        }

        #endregion
    }
}
