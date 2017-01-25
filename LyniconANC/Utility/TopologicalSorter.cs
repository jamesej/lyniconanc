using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Utility
{
    /// <summary>
    /// Extension methods to sort an enumerable so it satisfies a set of ordering criteria
    /// </summary>
    public static class TopologicalSorter
    {
        /// <summary>
        /// Sort an enumerable according to a function which indicates which items must come before each item
        /// </summary>
        /// <typeparam name="T">Type of the Enumerable</typeparam>
        /// <param name="items">The enumerable of items</param>
        /// <param name="fnItemsBeforeMe">Function that returns the list of items before each item in the enumerable</param>
        /// <returns>The sorted enumerable</returns>
        public static IEnumerable<T> Sort<T>(this IEnumerable<T> items, Func<T, IEnumerable<T>> fnItemsBeforeMe)
        {
            return Sort<T>(items, fnItemsBeforeMe, null);
        }
        /// <summary>
        /// Sort an enumerable according to a function which indicates which items must come before each item
        /// </summary>
        /// <typeparam name="T">Type of the Enumerable</typeparam>
        /// <param name="items">The enumerable of items</param>
        /// <param name="fnItemsBeforeMe">Function that returns the list of items before each item in the enumerable</param>
        /// <param name="comparer">Comparer to define equality between items</param>
        /// <returns>The sorted enumerable</returns>
        public static IEnumerable<T> Sort<T>(this IEnumerable<T> items, Func<T, IEnumerable<T>> fnItemsBeforeMe, IEqualityComparer<T> comparer)
        {
            HashSet<T> seen = comparer != null ? new HashSet<T>(comparer) : new HashSet<T>();
            HashSet<T> done = comparer != null ? new HashSet<T>(comparer) : new HashSet<T>();
            List<T> result = new List<T>();
            foreach (var item in items)
            {
                SortItem(item, fnItemsBeforeMe, seen, done, result);
            }
            return result;
        }

        private static void SortItem<T>(T item, Func<T, IEnumerable<T>> fnItemsBeforeMe, HashSet<T> seen, HashSet<T> done, List<T> result)
        {
            if (!done.Contains(item))
            {
                if (seen.Contains(item))
                {
                    throw new InvalidOperationException("Cycle in topological sort");
                }
                seen.Add(item);
                var itemsBefore = fnItemsBeforeMe(item);
                if (itemsBefore != null)
                {
                    foreach (var itemBefore in itemsBefore)
                    {
                        SortItem(itemBefore, fnItemsBeforeMe, seen, done, result);
                    }
                }
                result.Add(item);
                done.Add(item);
            }
        }
    }
}
