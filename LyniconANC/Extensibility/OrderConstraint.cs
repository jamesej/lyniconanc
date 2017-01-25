using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Utility;

namespace Lynicon.Extensibility
{
    public enum ConstraintType
    {
        /// <summary>
        /// Listed items must come before this one
        /// </summary>
        ItemsBefore,
        /// <summary>
        /// Listed items must come after this one
        /// </summary>
        ItemsAfter
    }

    /// <summary>
    /// Defines a partial order over a list of strings (which could be object ids) together with an API
    /// for combining multiple constraints into one
    /// </summary>
    public class OrderConstraint
    {
        private Dictionary<string, List<string>> itemItemsBefore = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> itemItemsAfter = new Dictionary<string, List<string>>();

        /// <summary>
        /// Create an order constraint with no order specified
        /// </summary>
        public OrderConstraint()
        {
        }
        /// <summary>
        /// Create an order constraint specifying what items have to come before a specific item
        /// </summary>
        /// <param name="name">the specific item (or its name)</param>
        /// <param name="itemsBefore">items before this item - uses namespace-style matching</param>
        public OrderConstraint(string name, params string[] itemsBefore)
        {
            itemItemsBefore.Add(name, itemsBefore.ToList());
        }
        /// <summary>
        /// Create an order constraint specifying what items have to come either before or after a specific item
        /// </summary>
        /// <param name="name">the specific item (or its name)</param>
        /// <param name="cType">whether the items must come before (ConstraintType.ItemsBefore) or after the specific item</param>
        /// <param name="items">items before/after this item - uses namespace-style matching</param>
        public OrderConstraint(string name, ConstraintType cType, params string[] items)
        {
            if (cType == ConstraintType.ItemsBefore)
                AddBefores(name, items);
            else
                AddAfters(name, items);
        }
        /// <summary>
        /// Create an order constraint with a list of items that have to come before and items that have to come after a specific item
        /// </summary>
        /// <param name="name">the specific item (or its name)</param>
        /// <param name="itemsBefore">the items that must come before it</param>
        /// <param name="itemsAfter">the items that must come after it</param>
        public OrderConstraint(string name, IEnumerable<string> itemsBefore, IEnumerable<string> itemsAfter)
        {
            AddBefores(name, itemsBefore);
            AddAfters(name, itemsAfter);
        }

        /// <summary>
        /// Add to this OrderConstraint rules on items that must come before a specific item
        /// </summary>
        /// <param name="name">The specific item (or its name)</param>
        /// <param name="itemsBefore">the items that must come before it</param>
        public void AddBefores(string name, IEnumerable<string> itemsBefore)
        {
            if (itemItemsBefore.ContainsKey(name))
                itemItemsBefore[name].AddRange(itemsBefore);
            else
                itemItemsBefore.Add(name, itemsBefore.ToList());
        }

        /// <summary>
        /// Add to this OrderConstraint rules on items that must come after a specific item
        /// </summary>
        /// <param name="name">The specific item (or its name)</param>
        /// <param name="itemsAfter">the items that must come after it</param>
        public void AddAfters(string name, IEnumerable<string> itemsAfter)
        {
            if (itemItemsAfter.ContainsKey(name))
                itemItemsAfter[name].AddRange(itemsAfter);
            else
                itemItemsAfter.Add(name, itemsAfter.ToList());
        }

        /// <summary>
        /// Adds the rules in another order constraint to this one
        /// </summary>
        /// <param name="other">The other order constraint from which to take rules</param>
        public void AddAnd(OrderConstraint other)
        {
            foreach (var kvp in other.itemItemsBefore)
            {
                AddBefores(kvp.Key, kvp.Value);
            }
            foreach (var kvp in other.itemItemsAfter)
            {
                AddAfters(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Clear all rules
        /// </summary>
        public void Clear()
        {
            itemItemsBefore.Clear();
            itemItemsAfter.Clear();
        }

        /// <summary>
        /// Get a function to use in a topological sort which given an item in a list, returns the items that come
        /// before it according to this orderconstraint over string ids of the items
        /// </summary>
        /// <typeparam name="T">The type of the items to be sorted</typeparam>
        /// <param name="nameSelector">A function to obtain a unique string id for each item (namespace style)</param>
        /// <param name="allItems">The list of all the items which will be sorted</param>
        /// <returns>A function returning for an item the ones that must come before it</returns>
        public Func<T, IEnumerable<T>> GetItemsBeforeFunc<T>(Func<T, string> nameSelector, List<T> allItems)
        {
            // items before each item indexed by name of item
            var itemDict = new Dictionary<string, List<T>>();
            // all items indexed by name
            var itemLookup = allItems.ToDictionary(i => nameSelector(i), i => i);
            // sorted list of names of all items
            var itemNames = itemLookup.Keys.ToArray();
            Array.Sort(itemNames);
            
            foreach (var item in allItems)
            {
                var name = nameSelector(item);
                var befores = new List<T>();
                if (itemItemsBefore.ContainsKey(name))
                    // Add items before this item to the itemDict list for this item
                    foreach (var bef in itemItemsBefore[name])
                    {
                        var pos = Array.BinarySearch<string>(itemNames, bef);
                        if (pos < 0)
                            pos = (~pos);
                        while (pos < itemNames.Length && (itemNames[pos] == bef || itemNames[pos].StartsWith(bef + ".")))
                            befores.Add(itemLookup[itemNames[pos++]]);
                    }
                if (itemDict.ContainsKey(name))
                    itemDict[name].AddRange(befores);
                else
                    itemDict.Add(name, befores);

                if (itemItemsAfter.ContainsKey(name))
                    // Add this item to the itemDict list for the items after it
                    foreach (var after in itemItemsAfter[name])
                    {
                        var pos = Array.BinarySearch<string>(itemNames, after);
                        if (pos < 0)
                            pos = (~pos);
                        while (pos < itemNames.Length && (itemNames[pos] == after || itemNames[pos].StartsWith(after + ".")))
                        {
                            if (itemDict.ContainsKey(itemNames[pos]))
                                itemDict[itemNames[pos]].Add(itemLookup[name]);
                            else
                                itemDict.Add(itemNames[pos], new List<T> { itemLookup[name] });
                            pos++;
                        }
                    }
            }

            return item => itemDict[nameSelector(item)];
        }
    }
}
