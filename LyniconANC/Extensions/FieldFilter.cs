using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Attributes;
using Lynicon.Extensibility;
using Lynicon.Models;
using Lynicon.Utility;
using System.Linq.Expressions;
using Lynicon.Relations;

namespace Lynicon.Extensions
{
    /// <summary>
    /// Abstract base class for filters based on properties of content types
    /// </summary>
    public abstract class FieldFilter : ListFilter
    {
        PropertyInfo propInfo = null;
        /// <summary>
        /// The property on which the filter operates
        /// </summary>
        public PropertyInfo PropInfo
        {
            get { return propInfo; }
            set
            {
                propInfo = value;
                if (string.IsNullOrEmpty(this.Name) && propInfo != null)
                    this.Name = propInfo.Name;
            }
        }

        /// <summary>
        /// Whether its possible to have the property value shown in the output
        /// </summary>
        public bool Showable { get; set; }

        /// <summary>
        /// Create an appropriate filter from an FieldFilterAttribute and a property it is applied to
        /// </summary>
        /// <param name="filterAttr">the FieldFilterAttribute</param>
        /// <param name="propInfo">the property it is applied to</param>
        /// <returns>Resulting filter</returns>
        public static ListFilter Create(FieldFilterAttribute filterAttr, PropertyInfo propInfo)
        {
            ListFilter filt = null;

            if (filterAttr.ReferencedType != null && filterAttr.ReferencedFieldName != null)
            {
                if (typeof(Reference).IsAssignableFrom(propInfo.PropertyType))
                    filt = new ReferenceFilter(filterAttr, propInfo);
                else
                    filt = new ForeignKeyFilter(filterAttr, propInfo);
            }
            else
            {
                switch (propInfo.PropertyType.Name)
                {
                    case "String":
                        filt = new StringFieldFilter(filterAttr, propInfo);
                        break;
                    case "DateTime":
                        filt = new DateFieldFilter(filterAttr, propInfo);
                        break;
                    case "Nullable`1":
                        if (Nullable.GetUnderlyingType(propInfo.PropertyType) == typeof(DateTime))
                            filt = new DateFieldFilter(filterAttr, propInfo);
                        break;
                }
            }
            if (filt == null)
                throw new NotImplementedException("Cannot have FieldFilter for property type " + propInfo.PropertyType.Name);

            ((FieldFilter)filt).Showable = filterAttr.Showable;

            return filt;
        }

        /// <summary>
        /// Create a FieldFilter
        /// </summary>
        public FieldFilter()
        {
            this.Showable = true;
        }
        /// <summary>
        /// Create a FieldFilter from a FieldFilterAttribute and the property it is attached to
        /// </summary>
        /// <param name="filterAttr">The FieldFilterAttribute</param>
        /// <param name="propInfo">The property the attribute is attached to</param>
        public FieldFilter(FieldFilterAttribute filterAttr, PropertyInfo propInfo) : this()
        {
            this.Name = filterAttr.Name ?? propInfo.Name;
            this.PropInfo = propInfo;
            this.ApplicableType = propInfo.DeclaringType;
        }
        /// <summary>
        /// Create a FieldFilter from a name and a property
        /// </summary>
        /// <param name="name">Name of a FieldFilter</param>
        /// <param name="propInfo">The property to build it from</param>
        public FieldFilter(string name, PropertyInfo propInfo) : this()
        {
            this.Name = name;
            this.PropInfo = propInfo;
            this.ApplicableType = propInfo.DeclaringType;
        }

        /// <summary>
        /// Merge the non user-edited values of a FieldFilter with this
        /// </summary>
        /// <param name="filt">FieldFilter containing non user-edited values</param>
        public override void MergeOriginal(ListFilter filt)
        {
            FieldFilter fFilt = filt as FieldFilter;

            this.PropInfo = fFilt.PropInfo;
            this.ApplicableType = fFilt.ApplicableType;
            this.Showable = fFilt.Showable;
        }

        /// <summary>
        /// Get the value of the property of this FieldFilter from results row data (for display)
        /// </summary>
        /// <param name="row">Results row data</param>
        /// <returns>The value of the property</returns>
        public object GetField(Tuple<object, Summary> row)
        {
            object val = null;
            if (typeof(Summary).IsAssignableFrom(this.ApplicableType) && this.ApplicableType.IsAssignableFrom(row.Item2.GetType()))
                val = this.PropInfo.GetValue(row.Item2);
            else if (this.ApplicableType.IsAssignableFrom(row.Item1.GetType()))
                val = this.PropInfo.GetValue(row.Item1);

            return val;
        }

        /// <summary>
        /// Apply a sort to a result row list based on the settings in this FieldFilter
        /// </summary>
        /// <param name="source">The result row list</param>
        /// <returns>The sorted result row list</returns>
        public override IEnumerable<Tuple<object, Summary>> ApplySort(IEnumerable<Tuple<object, Summary>> source)
        {
            return (IEnumerable<Tuple<object, Summary>>)ReflectionX.InvokeGenericMethod(this, "ApplySort", false, mi => true, new Type[] { PropInfo.PropertyType }, source);
        }
        /// <summary>
        /// Apply a sort to a result row list based on the settings in this FieldFilter
        /// </summary>
        /// <typeparam name="T">The type of the sort key</typeparam>
        /// <param name="source">The result row list</param>
        /// <returns>The sorted result row list</returns>
        protected IEnumerable<Tuple<object, Summary>> ApplySort<T>(IEnumerable<Tuple<object, Summary>> source)
        {
            Type tosType = typeof(Tuple<object, Summary>);
            var param = Expression.Parameter(tosType, "x");
            Expression paramProp;
            if (typeof(Summary).IsAssignableFrom(this.ApplicableType))
            {
                paramProp = Expression.MakeMemberAccess(param, tosType.GetProperty("Item2"));
            }   
            else
            {
                paramProp = Expression.MakeMemberAccess(param, tosType.GetProperty("Item1"));
            }

            var typedParam = Expression.TypeAs(paramProp, this.ApplicableType);
            var ifNull = Expression.Equal(typedParam, Expression.Constant(null));
            var typedNull = Expression.Default(PropInfo.PropertyType);
            var memberAccess = Expression.MakeMemberAccess(typedParam, PropInfo);
            var memberOrNull = Expression.Condition(ifNull, typedNull, memberAccess);
            var sortClause = Expression.Lambda<Func<Tuple<object, Summary>, T>>(memberOrNull, param);
            if (Sort > 0)
                return source.OrderBy(sortClause.Compile());
            else if (Sort < 0)
                return source.OrderByDescending(sortClause.Compile());
            else
                return source;
        }
    }
}
