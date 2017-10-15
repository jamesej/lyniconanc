using Lynicon.Extensibility;
using Lynicon.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Utility;
using Microsoft.EntityFrameworkCore;
using Lynicon.Services;

namespace Lynicon.DataSources
{
    public class NullDataSource : IDataSource
    {
        public string DataSourceSpecifier { get; set; }

        public LyniconSystem System { get; set; }

        public NullDataSource(LyniconSystem sys)
        {
            System = sys;
        }

        public void Create(object o)
        { }

        public void Delete(object o)
        { }

        public IQueryable GetSource(Type type)
        {
            var extType = System.Extender[type] ?? type;
            return Array.CreateInstance(extType, 0).AsQueryable();
        }

        public void SaveChanges()
        { }

        public void Update(object o)
        { }

        public void Dispose()
        { }
    }
}
