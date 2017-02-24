using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Collation;
using Lynicon.DataSources;
using Lynicon.Utility;
using System.Collections;

namespace LyniconANC.Test
{
    public class MockDataSource : IDataSource
    {
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<ItemId, object>> Data = new ConcurrentDictionary<Type, ConcurrentDictionary<ItemId, object>>();

        #region IDataSource Members

        public IQueryable GetSource(Type type)
        {
            Type t = type.ContentType();
            if (!Data.ContainsKey(t))
                return Array.CreateInstance(t, 0).AsQueryable();
            else
                return Data[t].Values.OfTypeRuntime(t).AsQueryable();
        }

        public void Update(object o)
        {
            if (o == null)
                return;

            Data[o.GetType().ContentType()][new ItemId(o)] = o;
        }

        public void Create(object o)
        {
            if (o == null)
                return;

            Type oType = o.GetType().ContentType();
            if (!Data.ContainsKey(oType))
                Data.TryAdd(oType, new ConcurrentDictionary<ItemId,object>());

            Data[oType].TryAdd(new ItemId(o), o);
        }

        public void Delete(object o)
        {
            if (o == null)
                return;

            if (!Data.ContainsKey(o.GetType().ContentType()))
                return;

            object remd;
            Data[o.GetType().ContentType()].TryRemove(new ItemId(o), out remd);
        }

        public void SaveChanges()
        { }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion
    }
}
