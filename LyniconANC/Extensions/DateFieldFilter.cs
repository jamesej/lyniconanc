using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Attributes;
using Lynicon.Models;

namespace Lynicon.Extensions
{
    /// <summary>
    /// Field filter which allows filtering by a date property
    /// </summary>
    public class DateFieldFilter : FieldFilter
    {
        /// <summary>
        /// Create a new DateFieldFilter
        /// </summary>
        public DateFieldFilter()
        { }
        /// <summary>
        /// Create a DateFieldFilter based on an attribute attached to a property
        /// </summary>
        /// <param name="filterAttr">The FieldFilterAttribute</param>
        /// <param name="propInfo">The property to which the attribute is attached</param>
        public DateFieldFilter(FieldFilterAttribute filterAttr, PropertyInfo propInfo)
            : base(filterAttr, propInfo)
        { }
        /// <summary>
        /// Create a DateFieldfilter with a given name applied to a property
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="propInfo">The property to filter on</param>
        public DateFieldFilter(string name, PropertyInfo propInfo)
            : base(name, propInfo)
        { }

        /// <summary>
        /// Filter form value for date range filter start
        /// </summary>
        public DateTime? From { get; set; }
        /// <summary>
        /// Filter form value for date range filter end
        /// </summary>
        public DateTime? To { get; set; }

        public override bool Active
        {
            get { return From.HasValue || To.HasValue; }
        }

        public override Func<IQueryable<T>, IQueryable<T>> Apply<T>()
        {
            var xParam = Expression.Parameter(typeof(T), "x");
            var accessProp = Expression.MakeMemberAccess(xParam, PropInfo);
            Expression toExp = null;
            Expression fromExp = null;
            if (PropInfo.PropertyType.IsGenericType && PropInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (From.HasValue)
                    fromExp = Expression.Coalesce(accessProp, Expression.Constant(new DateTime(3000, 1, 1)));
                if (To.HasValue)
                    toExp = Expression.Coalesce(accessProp, Expression.Constant(new DateTime(1900, 1, 1)));
            }
            else
            {
                fromExp = From.HasValue ? accessProp : null;
                toExp = To.HasValue ? accessProp : null;
            }
            Expression fromCond = null;
            Expression toCond = null;
            if (fromExp != null)
                fromCond = Expression.GreaterThanOrEqual(fromExp, Expression.Constant(From.Value));
            if (toExp != null)
                toCond = Expression.LessThanOrEqual(toExp, Expression.Constant(To.Value));
            Expression<Func<T, bool>> test = null;
            if (fromCond != null && toCond != null)
                test = Expression.Lambda<Func<T, bool>>(Expression.AndAlso(fromCond, toCond), xParam);
            else if (fromCond != null)
                test = Expression.Lambda<Func<T, bool>>(fromCond, xParam);
            else if (toCond != null)
                test = Expression.Lambda<Func<T, bool>>(toCond, xParam);

            return iq => iq.Where(test);
        }

        public override string GetShowText(Tuple<object, Summary> row)
        {
            object val = GetField(row);
            string prefix = (string.IsNullOrEmpty(this.Name) ? "" : this.Name.Substring(0, 1) + " ");
            return prefix + (val == null ? "" : ((DateTime)val).ToString("yyyy/MM/dd HH:mm"));
        }
    }
}
