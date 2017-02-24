using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Utility;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// Maintains a list of items with optional constraints on their positioning relative to other items, in an order
    /// satisfying all constraints if possible
    /// </summary>
    /// <typeparam name="T">The type of item</typeparam>
    public class ConstraintOrderedCollection<T> : ICollection<T>
    {
        private OrderConstraint orderConstraint { get; set; }
        private List<T> innerList { get; set; }
        private bool isOrdered = true;
        private Func<T, string> nameSelector;

        /// <summary>
        /// Construct a new ConstraintOrderedCollection with a selector for the string used to identify
        /// items in constraint rules
        /// </summary>
        /// <param name="nameSelector">Function which creates the name string from an item</param>
        public ConstraintOrderedCollection(Func<T, string> nameSelector)
        {
            orderConstraint = new OrderConstraint();
            this.nameSelector = nameSelector;
            innerList = new List<T>();
        }
        public ConstraintOrderedCollection()
            : this(item => item.ToString())
        {
        }

        /// <summary>
        /// Copy a ConstraintOrderedCollection with all its constraints
        /// </summary>
        /// <returns>The copied ConstraintOrderedCollection</returns>
        public ConstraintOrderedCollection<T> Copy()
        {
            var coc = new ConstraintOrderedCollection<T>(this.nameSelector);
            coc.innerList.AddRange(this.innerList);
            coc.orderConstraint.AddAnd(this.orderConstraint);
            return coc;
        }

        #region ICollection<T> Members

        public void Add(T item)
        {
            innerList.Add(item);
            isOrdered = false;
        }
        /// <summary>
        /// Add a new item with a constraint rule
        /// </summary>
        /// <param name="item">The item to add</param>
        /// <param name="constraint">The constraint rule</param>
        public void Add(T item, OrderConstraint constraint)
        {
            Add(item);
            orderConstraint.AddAnd(constraint);
        }
        /// <summary>
        /// Add a new item specifying the names of the items which must come before it (if they are present)
        /// </summary>
        /// <param name="item">The item</param>
        /// <param name="itemsBefore">The names of the items which must precede the item</param>
        public void Add(T item, params string[] itemsBefore)
        {
            Add(item, new OrderConstraint(nameSelector(item), itemsBefore));
        }
        /// <summary>
        /// Add a new item specifying the names of items which must come before or after it (if they are present)
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <param name="constraintType">Whether named items must come before or after this one</param>
        /// <param name="items">The names of items which must come before or after this one</param>
        public void Add(T item, ConstraintType constraintType, params string[] items)
        {
            Add(item, new OrderConstraint(nameSelector(item), constraintType, items));
        }
        /// <summary>
        /// Add a new item specifying a list of name of items which must come before it and a list of those which must come after it
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <param name="itemsBefore">Names of items which must come before it</param>
        /// <param name="itemsAfter">Names of items which must come after it</param>
        public void Add(T item, IEnumerable<string> itemsBefore, IEnumerable<string> itemsAfter)
        {
            Add(item, new OrderConstraint(nameSelector(item), itemsBefore, itemsAfter));
        }

        public void Clear()
        {
            innerList.Clear();
            orderConstraint.Clear();
            isOrdered = true;
        }

        public bool Contains(T item)
        {
            return innerList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            var enumr = GetEnumerator();
            while (enumr.MoveNext())
                array[arrayIndex++] = enumr.Current;
        }

        public int Count
        {
            get { return innerList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            if (innerList.Remove(item))
            {
                // removal doesn't break order
                // isOrdered = false;
                return true;
            }
            return false;
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            if (!isOrdered)
            {
                innerList = innerList
                    .Sort(orderConstraint.GetItemsBeforeFunc(nameSelector, innerList))
                    .ToList();
                isOrdered = true;
            }
            return innerList.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator() as System.Collections.IEnumerator;
        }

        #endregion
    }
}
