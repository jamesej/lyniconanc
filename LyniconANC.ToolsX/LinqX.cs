using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Collections.Specialized;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Collections;

namespace Lynicon.Tools
{
    public static class LinqX
    {
        public static PropertyInfo GetIdProp<T>()
        {
            return GetIdProp<T>(null);
        }
        public static PropertyInfo GetIdProp<T>(string idName)
        {
            return GetIdProp(typeof(T), idName);
        }
        public static PropertyInfo GetIdProp(Type t, string idName)
        {
            if (idName != null)
                return t.GetProperty(idName);

            var keyProp = t.GetProperties().FirstOrDefault(pi => pi.GetCustomAttribute<KeyAttribute>() != null);
            if (keyProp == null)
                keyProp = t.GetProperty("Id");
            if (keyProp == null)
                throw new Exception("Can't find Id Name of " + t.FullName);
            return keyProp;
        }

        public static Expression<Func<T, bool>> GetIdTest<T>(object id)
        {
            var idPi = GetIdProp<T>();
            var xParam = Expression.Parameter(typeof(T), "x");
            object idTrans = id;
            if (id is string && idPi.PropertyType != typeof(string))
            {
                TypeConverter typeConverter = TypeDescriptor.GetConverter(idPi.PropertyType);
                idTrans = typeConverter.ConvertFromString(id as string);
            }
            return Expression.Lambda(
                Expression.Equal(
                    Expression.MakeMemberAccess(xParam, idPi),
                    Expression.Constant(idTrans)),
                xParam) as Expression<Func<T, bool>>;
        }

        public static Expression<Func<T, bool>> GetPropertyTest<T>(string propName, object val)
        {
            var pi = typeof(T).GetProperty(propName);
            var xParam = Expression.Parameter(typeof(T), "x");
            object valTrans = val;
            if (val is string && pi.PropertyType != typeof(string))
            {
                TypeConverter typeConverter = TypeDescriptor.GetConverter(pi.PropertyType);
                valTrans = typeConverter.ConvertFromString(val as string);
            }
            return Expression.Lambda(
                Expression.Equal(
                    Expression.MakeMemberAccess(xParam, pi),
                    Expression.Constant(valTrans)),
                xParam) as Expression<Func<T, bool>>;
        }

        public static LambdaExpression GetIdSelector<T>()
        {
            var idPi = GetIdProp<T>();
            var xParam = Expression.Parameter(typeof(T), "x");
            return Expression.Lambda(
                    Expression.MakeMemberAccess(xParam, idPi),
                xParam);
        }
        public static LambdaExpression GetIdSelector(Type t)
        {
            var idPi = GetIdProp(t, null);
            var xParam = Expression.Parameter(t, "x");
            return Expression.Lambda(
                    Expression.MakeMemberAccess(xParam, idPi),
                xParam);
        }

        public static LambdaExpression GetFieldSelector<T>(string fieldName)
        {
            return GetFieldSelector(typeof(T), fieldName);
        }
        public static LambdaExpression GetFieldSelector(Type t, string fieldName)
        {
            var pi = t.GetProperty(fieldName);
            var xParam = Expression.Parameter(t, "x");
            return Expression.Lambda(
                    Expression.MakeMemberAccess(xParam, pi),
                xParam);
        }

        public static LambdaExpression GetPathSelector(Type baseType, string path)
        {
            var xParam = Expression.Parameter(baseType, "x");
            Expression pathAccess = xParam;
            Type elType = baseType;
            foreach (string pathEl in path.Split('.'))
            {
                string propName = pathEl.UpTo("[");
                var pi = elType.GetProperty(propName);
                pathAccess = Expression.MakeMemberAccess(pathAccess, pi);

                if (pathEl.Contains("["))
                {
                    int index = int.Parse(pathEl.After("[").UpTo("]"));
                    pathAccess = Expression.ArrayAccess(pathAccess, Expression.Constant(index));
                }
            }
            return Expression.Lambda(pathAccess, xParam);
        }

        public static LambdaExpression GetFieldProjector(Type tableType, Type projType, List<string> fields)
        {
            var param = Expression.Parameter(tableType, "x");
            var newExp = Expression.New(projType.GetConstructor(Type.EmptyTypes));
            var mInitExp = Expression.MemberInit(newExp,
                fields.Select(f => 
                    Expression.Bind(projType.GetProperty(f), Expression.MakeMemberAccess(param, tableType.GetProperty(f)))));
            return Expression.Lambda(mInitExp, param);
        }

        public static LambdaExpression GetFieldProjector(Type tableType, Type projType, Dictionary<string, string> projectionMap)
        {
            var param = Expression.Parameter(tableType, "x");
            var newExp = Expression.New(projType.GetConstructor(Type.EmptyTypes));
            var mInitExp = Expression.MemberInit(newExp,
                projectionMap.Select(kvp =>
                    Expression.Bind(projType.GetProperty(kvp.Value), Expression.MakeMemberAccess(param, tableType.GetProperty(kvp.Key)))));
            return Expression.Lambda(mInitExp, param);
        }

        public static IEnumerable OfTypeRuntime(this IEnumerable iq, Type type)
        {
            var ofType = typeof(Enumerable).GetMethod("OfType").MakeGenericMethod(type);
            return (IEnumerable)ofType.Invoke(null, new object[] { iq });
        }

        public static IEnumerable EmptyRuntime(Type type)
        {
            var ieType = typeof(List<>).MakeGenericType(type);
            return (IEnumerable)Activator.CreateInstance(ieType);
        }

        /// <summary>
        /// If a list is nonempty, passes it though unchanged.  If it is empty, substitutes it with another list.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="subst"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> IfEmpty<TSource>(this IEnumerable<TSource> source, IEnumerable<TSource> subst)
        {
            bool isEmpty = true;
            foreach (TSource element in source)
            {
                isEmpty = false;
                yield return element;
            }
            if (isEmpty)
                foreach (TSource element in subst)
                    yield return element;
        }

        public static TResult Max<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector, TResult ifEmpty)
        {
            if (source.Any<TSource>())
                return source.Max(selector);
            else
                return ifEmpty;
        }

        public static IEnumerable<TSource> Concat<TSource>(this IEnumerable<TSource> source, TSource item)
        {
            if (source != null)
                foreach (TSource element in source)
                    yield return element;
            // don't append item if it's default for the type TSource
            if (!EqualityComparer<TSource>.Default.Equals(item,default(TSource)))
                yield return item;
        }

        public static Dictionary<TKey, TVal> ToDictionary<TSource, TKey, TVal>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TVal> valueSelector, Action<TSource> dupKeyAction)
        {
            var d = new Dictionary<TKey, TVal>();
            foreach (TSource item in source)
            {
                if (d.ContainsKey(keySelector(item)))
                    dupKeyAction(item);
                else
                    d.Add(keySelector(item), valueSelector(item));
            }

            return d;
        }

        public static IQueryable<T> OrderByField<T>(this IQueryable<T> source, string fieldName)
        {
            PropertyInfo pi = typeof(T).GetProperty(fieldName);
            var mInfo = typeof(Queryable).GetMethods().Where(mi => mi.Name == "OrderBy" && mi.GetParameters().Length == 2).Single();
            var mInfoGen = mInfo.MakeGenericMethod(typeof(T), pi.PropertyType);
            
            return (IQueryable<T>)mInfoGen.Invoke(null, new object[] {
                source,
                LinqX.GetFieldSelector<T>(fieldName)});
        }

        private static Expression<Func<TElement, bool>> GetWhereInExpression<TElement, TValue>(Expression<Func<TElement, TValue>> propertySelector, IEnumerable<TValue> values)
        {
            ParameterExpression p = propertySelector.Parameters.Single();
            if (values == null || !values.Any())
                return e => false;

            var equals = values.Select(value => (Expression)Expression.Equal(propertySelector.Body, Expression.Constant(value, typeof(TValue))));
            var body = equals.Aggregate<Expression>((accumulate, equal) => Expression.Or(accumulate, equal));

            return Expression.Lambda<Func<TElement, bool>>(body, p);
        }
        private static Expression<Func<TElement, bool>> GetWhereInExpression<TElement>(LambdaExpression propertySelector, IEnumerable<object> values)
        {
            return GetWhereInExpression<TElement>(propertySelector, values, null);
        }
        private static Expression<Func<TElement, bool>> GetWhereInExpression<TElement>(LambdaExpression propertySelector, IEnumerable<object> values, Type itemType)
        {
            ParameterExpression p = propertySelector.Parameters.Single();
            if (values == null || !values.Any())
                return e => false;

            var equals = values.Select(value => (Expression)Expression.Equal(propertySelector.Body, Expression.Constant(value, itemType == null ? (value == null ? typeof(object) : value.GetType()) : itemType)));
            var body = equals.Aggregate<Expression>((accumulate, equal) => Expression.Or(accumulate, equal));

            return Expression.Lambda<Func<TElement, bool>>(body, p);
        }
        private static Expression<Func<TElement, bool>> GetWhereInExpression<TElement, TValue>(Expression<Func<TElement, TValue>> propertySelector, IEnumerable<TValue> values, bool isInclude)
        {
            ParameterExpression p = propertySelector.Parameters.Single();
            if (values == null || !values.Any())
                return e => false;

            var equals = values.Select(value => (Expression)Expression.Equal(propertySelector.Body, Expression.Constant(value, typeof(TValue))));
            var body = equals.Aggregate<Expression>((accumulate, equal) => Expression.Or(accumulate, equal));
            if (!isInclude)
                body = Expression.Not(body);

            return Expression.Lambda<Func<TElement, bool>>(body, p);
        }

        /// <summary> 
        /// Return the element that the specified property's value is contained in the specific values 
        /// </summary> 
        /// <typeparam name="TElement">The type of the element.</typeparam> 
        /// <typeparam name="TValue">The type of the values.</typeparam> 
        /// <param name="source">The source.</param> 
        /// <param name="propertySelector">The property to be tested.</param> 
        /// <param name="values">The accepted values of the property.</param> 
        /// <returns>The accepted elements.</returns> 
        public static IQueryable<TElement> WhereIn<TElement, TValue>(this IQueryable<TElement> source, Expression<Func<TElement, TValue>> propertySelector, params TValue[] values)
        {
            return source.Where(GetWhereInExpression(propertySelector, values));
        }

        /// <summary> 
        /// Return the element that the specified property's value is contained in the specific values 
        /// </summary> 
        /// <typeparam name="TElement">The type of the element.</typeparam> 
        /// <typeparam name="TValue">The type of the values.</typeparam> 
        /// <param name="source">The source.</param> 
        /// <param name="propertySelector">The property to be tested.</param> 
        /// <param name="values">The accepted values of the property.</param> 
        /// <returns>The accepted elements.</returns> 
        public static IQueryable<TElement> WhereIn<TElement, TValue>(this IQueryable<TElement> source, Expression<Func<TElement, TValue>> propertySelector, IEnumerable<TValue> values)
        {
            return source.Where(GetWhereInExpression(propertySelector, values));
        }
        /// <summary>
        /// Return the element that the specified property's value is contained in the specific values
        /// Use for IEnumerable object to avoid boxing issues
        /// </summary>
        /// <typeparam name="TElement">the type of the element</typeparam>
        /// <param name="source">the source list</param>
        /// <param name="propertySelector">The property to be tested.  Must provide type information as cannot be inferred.</param>
        /// <param name="values">the values (as objects)</param>
        /// <returns>The accepted elements</returns>
        public static IQueryable<TElement> WhereIn<TElement>(this IQueryable<TElement> source, LambdaExpression propertySelector, IEnumerable<object> values)
        {
            return source.Where(GetWhereInExpression<TElement>(propertySelector, values));
        }

        public static IQueryable<TElement> WhereIn<TElement>(this IQueryable<TElement> source, LambdaExpression propertySelector, IEnumerable<object> values, Type itemType)
        {
            return source.Where(GetWhereInExpression<TElement>(propertySelector, values, itemType));
        }

        public static IQueryable<TElement> WhereNotIn<TElement, TValue>(this IQueryable<TElement> source, Expression<Func<TElement, TValue>> propertySelector, IEnumerable<TValue> values)
        {
            return source.Where(GetWhereInExpression<TElement, TValue>(propertySelector, values, false));
        }

        public static void Do<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T item in source)
                action(item);
        }
        public static void Do<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            int i = 0;
            foreach (T item in source)
                action(item, i++);
        }

        public static string Join<T>(this IEnumerable<T> source, string separator)
        {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (T item in source)
            {
                if (first)
                    first = false;
                else
                    sb.Append(separator);
                sb.Append(item.ToString());
            }
            return sb.ToString();
        }

        public static IEnumerable<KeyValuePair<string, string>> ToKeyValues(this NameValueCollection nvc)
        {
            if (nvc == null)
                return new KeyValuePair<string, string>[] { };
            return nvc.Cast<string>().SelectMany(key => (nvc[key] ?? "").Split(',').Select(val => new KeyValuePair<string, string>(key, val)));
        }

        public static TValue FirstSelectOrDefault<TElement, TValue>(this IEnumerable<TElement> source, Func<TElement, bool> test, Func<TElement, TValue> selector)
        {
            TElement el = source.FirstOrDefault(test);
            if (el == null || el.Equals(default(TElement)))
                return default(TValue);
            else
                return selector(el);
        }
        public static TValue FirstSelectOrDefault<TElement, TValue>(this IQueryable<TElement> source, Func<TElement, bool> test, Func<TElement, TValue> selector)
        {
            TElement el = source.FirstOrDefault(test);
            if (el.Equals(default(TElement)))
                return default(TValue);
            else
                return selector(el);
        }

        public static IEnumerable<T> TraverseDepthFirst<T>(this T root, Func<T, IEnumerable<T>> getChildren, Func<T, bool> selector)
        {
            if (selector(root))
                yield return root;
            foreach (T child in getChildren(root))
                foreach (T found in TraverseDepthFirst(child, getChildren, selector))
                    yield return found;
        }


        public static IEnumerable<TSource> PartialOrderBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
        {

            return PartialOrderBy(source, keySelector, null);
        }

        public static IEnumerable<TSource> PartialOrderBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IComparer<TKey> comparer)   // the '0' value of this comparer is interpreted as 'don't know relationship'
        {
            if (source == null) throw new ArgumentNullException("source");
            if (keySelector == null) throw new ArgumentNullException("keySelector");
            if (comparer == null) comparer = (IComparer<TKey>)Comparer<TKey>.Default;

            return PartialOrderByIterator(source, keySelector, comparer);
        }

        private static IEnumerable<TSource> PartialOrderByIterator<TSource, TKey>(
            IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IComparer<TKey> comparer)
        {
            var values = source.ToArray();
            var keys = values.Select(keySelector).ToArray();
            int count = values.Length;
            var notYieldedIndexes = System.Linq.Enumerable.Range(0, count).ToArray();
            int valuesToGo = count;

            while (valuesToGo > 0)
            {
                //Start with first value not yielded yet
                int minIndex = notYieldedIndexes.First(i => i >= 0);

                //Find minimum value amongst the values not yielded yet
                for (int i = 0; i < count; i++)
                    if (notYieldedIndexes[i] >= 0)
                        if (comparer.Compare(keys[i], keys[minIndex]) < 0)
                        {
                            minIndex = i;
                        }

                //Yield minimum value and mark it as yielded
                yield return values[minIndex];
                notYieldedIndexes[minIndex] = -1;
                valuesToGo--;
            }
        }

        //public static IEnumerable<List<T>> CrossProduct<T>(this IEnumerable<T> source, IEnumerable<T> crossWith)
        //{
        //    foreach (T item in source)
        //        foreach (T crossItem in crossWith)
        //            yield return new List<T> { item, crossItem };
        //}
        public static IEnumerable<List<T>> PartialCrossProduct<T>(this IEnumerable<List<T>> source, IEnumerable<T> crossWith)
        {
            foreach (List<T> partial in source)
                foreach (T crossItem in crossWith)
                    yield return partial.Concat(crossItem).ToList();
        }
        public static IEnumerable<List<T>> CrossProduct<T>(this IEnumerable<List<T>> source, IEnumerable<List<T>> crossWith)
        {
            foreach (List<T> item in source)
                foreach (List<T> crossItem in crossWith)
                    yield return item.Concat(crossItem).ToList();
        }

        public static int IndexOfPredicate<T>(this IEnumerable<T> source, Func<T, bool> pred)
        {
            int i = 0;
            foreach (T item in source)
            {
                if (pred(item))
                    return i;
                i++;
            }
            return -1;
        }

        public static int IndexOfLastPredicate<T>(this IEnumerable<T> source, Func<T, bool> pred)
        {
            int i = 0;
            int iLast = -1;
            foreach (T item in source)
            {
                if (pred(item))
                    iLast = i;
                i++;
            }
            return iLast;
        }

        public static IEnumerable<List<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            var batch = new List<T>();
            foreach (T item in source)
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    yield return batch;
                    batch = new List<T>();
                }
            }
            if (batch.Count > 0)
                yield return batch;
        }

        public static IEnumerable<T> ShufflePositions<T>(this IEnumerable<T> source, int[] positions)
        {
            T[] buffer = new T[positions.Length];
            T[] outBuffer = new T[positions.Length];

            int i = 0;
            int j = 0;
            foreach (T item in source)
            {
                if (0 <= i && i < positions.Length)
                {
                    buffer[i++] = item;
                    if (i == positions.Length)
                    {
                        i = -1;
                        foreach (int pos in positions)
                            outBuffer[pos] = buffer[j++];
                        for (int k = 0; k < j; k++)
                            yield return outBuffer[k];
                    }
                }
                else
                    yield return item;
            }

            if (i > 0)
            {
                foreach (int pos in positions)
                {
                    if (pos >= i)
                        continue;
                    outBuffer[pos] = buffer[j++];
                }
                for (int k = 0; k < j; k++)
                    yield return outBuffer[k];
            }

        }


    }
}
