using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Attributes;
using Lynicon.Extensibility;
using Lynicon.Models;
using Lynicon.Utility;

namespace Lynicon.Extensions
{
    /// <summary>
    /// A filter to use for string fields
    /// </summary>
    public class StringFieldFilter : FieldFilter
    {
        public StringFieldFilter()
        { }
        public StringFieldFilter(FieldFilterAttribute filterAttr, PropertyInfo propInfo)
            : base(filterAttr, propInfo)
        { }

        /// <summary>
        /// The user entered search value to match the string field
        /// </summary>
        public string Search { get; set; }

        public override Func<IQueryable<T>, IQueryable<T>> Apply<T>()
        {
            // encodes iq => iq.Where(
            var srch = Search.ToLower();
            var xParam = Expression.Parameter(typeof(T), "x");
            var accessProp = Expression.MakeMemberAccess(xParam, PropInfo);
            var coalescedString = Expression.Coalesce(accessProp, Expression.Constant(""));
            var toLowerMethod = typeof(string).GetMethod("ToLower", new Type[0]);
            var toLower = Expression.Call(coalescedString, toLowerMethod);
            var containsMethod = typeof(string).GetMethod("Contains");
            var srchParam = Expression.Constant(srch);
            var contains = Expression.Call(toLower, containsMethod, srchParam);
            var test = Expression.Lambda<Func<T, bool>>(contains, xParam);
            return iq => iq.Where(test);
        }

        public override bool Active
        {
            get { return !string.IsNullOrEmpty(Search); }
        }

        public override string GetShowText(Tuple<object, Summary> row)
        {
            object val = GetField(row);
            return val == null ? "" : (string)val;
        }
    }
}
