using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.DataSources;
using Lynicon.Services;

namespace LyniconANC.Test
{
    public class MockDataSourceFactory : IDataSourceFactory
    {
        #region IDataSourceFactory Members

        public string DataSourceSpecifier
        {
            get { return ""; }
        }

        public LyniconSystem System { get; set; }

        public MockDataSourceFactory(LyniconSystem sys)
        {
            System = sys;
        }

        public IDataSource Create(bool forSummaries)
        {
            return new MockDataSource(System);
        }

        #endregion
    }
}
