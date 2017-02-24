using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
    using System.Linq.Expressions;
using System.Collections;
using System.Reflection;
using System.ComponentModel.DataAnnotations.Schema;
using Lynicon.Utility;

namespace Lynicon.Linq
{
    /// <summary>
    /// Queryable which manages and retains data necessary for facading
    /// </summary>
    /// <typeparam name="T">Item type</typeparam>
    public class FacadeTypeQueryable<T> : IOrderedQueryable<T>
    {
        private readonly Expression _expression;
        private readonly FacadeTypeQueryProvider<T> _provider;
        private Type _from;
        private Type _to;
        private Dictionary<string, string> propertyMap;

        public FacadeTypeQueryable(IQueryable source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            _from = source.ElementType;
            _to = typeof(T);
            // start a new expression based on this
            _expression = Expression.Constant(this);
            _provider = new FacadeTypeQueryProvider<T>(source);
            propertyMap = new Dictionary<string, string>();
        }
        public FacadeTypeQueryable(IQueryable source, Dictionary<string, string> propertyMap)
            : this(source)
        {
            this.propertyMap = propertyMap;
        }

        public FacadeTypeQueryable(IQueryable source, Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            _from = source.ElementType;
            _to = typeof(T);
            _expression = expression;
            _provider = new FacadeTypeQueryProvider<T>(source);
            propertyMap = new Dictionary<string, string>();
        }
        public FacadeTypeQueryable(IQueryable source, Expression expression, Dictionary<string, string> propertyMap)
            : this(source, expression)
        {
            this.propertyMap = propertyMap;
        }

        /// <summary>
        /// Create a new item from an arbitrary object by copying same-named properties where possible
        /// </summary>
        /// <param name="o">Arbitrary object</param>
        /// <returns>Instance of T</returns>
        public T ConvertByProperties(object o)
        {
            if (o is T)
                return (T)o;

            T res = Activator.CreateInstance<T>();
            Type type = typeof(T);
            foreach (var pi in type.GetPersistedProperties())
            {
                var oPi = o.GetType().GetProperty(pi.Name);
                if (oPi == null || pi.PropertyType != oPi.PropertyType)
                    throw new Exception("Equal type property " + pi.Name + " does not exist on type " + o.GetType().FullName + ", cannot translate from " + type.FullName);
                pi.SetValue(res, oPi.GetValue(o));
            }
            return res;
        }

        /// <summary>
        /// Convert an IEnumerable to type T by converting its items by mapping property names
        /// </summary>
        /// <param name="iEnum">IEnumerable to convert</param>
        /// <returns>IEnumerable converted to one of type T</returns>
        private IEnumerable<T> MapConvert(IEnumerable iEnum)
        {
            foreach (var o in iEnum)
                yield return ConvertByProperties(o);
        }

        public IEnumerator<T> GetEnumerator()
        {
            var iEnum = _provider.ExecuteEnumerable(_expression);
            if (iEnum.GetType().GetGenericArguments()[0] == _from)
            {
                if (typeof(T).IsAssignableFrom(_from))
                    return iEnum.Cast<T>().GetEnumerator();
                else
                    return MapConvert(iEnum).GetEnumerator();
            }
            else
                return ((IEnumerable<T>)iEnum).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _provider.ExecuteEnumerable(_expression).GetEnumerator();
        }

        public Type ElementType
        {
            get { return typeof(T); }
        }

        public Expression Expression
        {
            get { return _expression; }
        }

        public IQueryProvider Provider
        {
            get { return _provider; }
        }

        public override string ToString()
        {
            return _provider.CreateUnderlyingQuery(_expression).ToString();
        }
    }
}
