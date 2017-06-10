using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Collation;
using Lynicon.DataSources;
using Lynicon.Utility;
using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using Lynicon.Repositories;
using Lynicon.Services;

namespace LyniconANC.Test
{
    public class MockDataSource : IDataSource
    {
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<object, object>> Data = new ConcurrentDictionary<Type, ConcurrentDictionary<object, object>>();

        public LyniconSystem System { get; set; }

        public MockDataSource(LyniconSystem sys)
        {
            System = sys;
        }

        #region IDataSource Members

        public IQueryable GetSource(Type type)
        {
            Type t = type.UnextendedType();
            Type extT = System.Extender[t] ?? t;
            if (!Data.ContainsKey(t))
                return Array.CreateInstance(extT, 0).AsQueryable();
            else
                return Data[t].Values.OfTypeRuntime(extT).AsQueryable();
        }

        private object GetKeyForCreate(Type oType, object o)
        {
            var keyInfo = LinqX.GetIdProp(o.GetType(), null);
            if (keyInfo.GetCustomAttribute<DatabaseGeneratedAttribute>()?.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity)
                keyInfo.SetValue(o, Data.ContainsKey(oType) ? Data[oType].Count + 1 : 1);
            return keyInfo.GetValue(o);
        }
        private object GetKey(object o)
        {
            return LinqX.GetIdProp(o.GetType(), null).GetValue(o);
        }

        public void Update(object o)
        {
            if (o == null)
                return;

            Data[o.GetType().UnextendedType()][GetKey(o)] = o;
        }

        public void Create(object o)
        {
            if (o == null)
                return;
            Type oType = o.GetType().UnextendedType();

            if (!Data.ContainsKey(oType))
                Data.TryAdd(oType, new ConcurrentDictionary<object, object>());

            Data[oType].TryAdd(GetKeyForCreate(oType, o), o);
        }

        public void Delete(object o)
        {
            if (o == null)
                return;

            if (!Data.ContainsKey(o.GetType().UnextendedType()))
                return;

            object remd;
            Data[o.GetType().UnextendedType()].TryRemove(GetKey(o), out remd);
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
