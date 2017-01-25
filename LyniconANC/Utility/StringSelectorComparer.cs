using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Utility
{
    /// <summary>
    /// Compare using a selector to extract a string value which is compared
    /// </summary>
    /// <typeparam name="T">Type of objects to compare</typeparam>
    public class StringSelectorComparer<T> : IEqualityComparer<T>, IComparer<T>
    {
        private Func<T, string> selector;
        private StringComparer innerComparer;

        /// <summary>
        /// Create a Comparer using a selector from the compared type to a string which is then compared, and a flag
        /// for whether that comparison ignores case
        /// </summary>
        /// <param name="selector">The selector from the type to a string</param>
        /// <param name="ignoreCase">Whether the string comparison should ignore case</param>
        public StringSelectorComparer(Func<T, string> selector, bool ignoreCase)
        {
            this.selector = selector;
            innerComparer = ignoreCase ? StringComparer.CurrentCultureIgnoreCase : StringComparer.CurrentCulture;
        }
        /// <summary>
        /// Create a Comparer using a selector from the compared type to a string which is then compared, a culture
        /// to use for this comparison, and a flag for whether the comparison ignores case
        /// </summary>
        /// <param name="selector">The selector from the type to a string</param>
        /// <param name="compareCult">The culture to use to make the string comparison</param>
        /// <param name="ignoreCase">Whether the string comparison should ignore case</param>
        public StringSelectorComparer(Func<T, string> selector, CultureInfo compareCult, bool ignoreCase)
        {
            this.selector = selector;
            innerComparer = StringComparer.Create(compareCult, ignoreCase);
        }

        #region IEqualityComparer<T> Members

        public bool Equals(T x, T y)
        {
            return Compare(x, y) == 0;
        }

        public int GetHashCode(T obj)
        {
            return selector(obj).GetHashCode();
        }

        #endregion

        #region IComparer<T> Members

        public int Compare(T x, T y)
        {
            return innerComparer.Compare(selector(x), selector(y));
        }

        #endregion
    }
}
