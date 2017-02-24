using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.DataSources
{
    public interface IDataSource : IDisposable
    {
        IQueryable GetSource(Type type);
        void Update(object o);
        void Create(object o);
        void Delete(object o);
        void SaveChanges();
    }
}
