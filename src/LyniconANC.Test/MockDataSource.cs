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

namespace LyniconANC.Test
{
    public class MockDataSource : IDataSource
    {
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<ItemId, object>> Data = new ConcurrentDictionary<Type, ConcurrentDictionary<ItemId, object>>();

        #region IDataSource Members

        public IQueryable GetSource(Type type)
        {
            Type t = type.UnextendedType();
            var extTypes = CompositeTypeManager.Instance.ExtendedTypes;
            Type extT = extTypes.ContainsKey(t) ? extTypes[t] : t;
            if (!Data.ContainsKey(t))
                return Array.CreateInstance(extT, 0).AsQueryable();
            else
                return Data[t].Values.OfTypeRuntime(extT).AsQueryable();
        }

        public void Update(object o)
        {
            if (o == null)
                return;

            Data[o.GetType().UnextendedType()][new ItemId(o)] = o;
        }

        public void Create(object o)
        {
            if (o == null)
                return;
            Type oType = o.GetType().UnextendedType();

            // Update identity key to next identity value if appropriate
            var keyInfo = LinqX.GetIdProp(o.GetType(), null);
            if (keyInfo.GetCustomAttribute<DatabaseGeneratedAttribute>()?.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity)
                keyInfo.SetValue(o, Data.ContainsKey(oType) ? Data[oType].Count + 1 : 1);

            if (!Data.ContainsKey(oType))
                Data.TryAdd(oType, new ConcurrentDictionary<ItemId,object>());

            Data[oType].TryAdd(new ItemId(o), o);
        }

        public void Delete(object o)
        {
            if (o == null)
                return;

            if (!Data.ContainsKey(o.GetType().UnextendedType()))
                return;

            object remd;
            Data[o.GetType().UnextendedType()].TryRemove(new ItemId(o), out remd);
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
