using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Attributes;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Models;
using Lynicon.Utility;
using Lynicon.Relations;

namespace Lynicon.Extensions
{
    /// <summary>
    /// A filter to use for filtering (or displaying) by a Reference-typed field
    /// </summary>
    public class ReferenceFilter : FieldFilter
    {
        private string refFieldName;
        private Type refType;

        /// <summary>
        /// Create a new ReferenceFilter
        /// </summary>
        public ReferenceFilter()
        {
            RefValue = new Reference();
        }
        /// <summary>
        /// Create a new ReferenceFilter from a FieldFilterAttribute and a property
        /// </summary>
        /// <param name="filterAttr">The FieldFilterAttribute</param>
        /// <param name="propInfo">The property</param>
        public ReferenceFilter(FieldFilterAttribute filterAttr, PropertyInfo propInfo)
            : base(filterAttr, propInfo)
        {
            refFieldName = filterAttr.ReferencedFieldName;
            refType = filterAttr.ReferencedType;
            RefValue = (Reference)Activator.CreateInstance(propInfo.PropertyType);
        }

        /// <summary>
        /// The user entered reference value
        /// </summary>
        public Reference RefValue { get; set; }

        public override Func<IQueryable<T>, IQueryable<T>> Apply<T>()
        {
            // encodes x => x.Prop.ToString() == '<serialized ref>'
            var xParam = Expression.Parameter(typeof(T), "x");
            var accessRef = Expression.MakeMemberAccess(xParam, this.PropInfo);
            MethodInfo toStringMeth = PropInfo.PropertyType.GetMethod("ToString");
            var propAsString = Expression.Call(accessRef, toStringMeth);
            var comp = Expression.Equal(propAsString, Expression.Constant(RefValue.ToString()));

            var selector = Expression.Lambda<Func<T, bool>>(comp, xParam);

            return iq => iq.Where(selector);
        }

        public override bool Active
        {
            get { return RefValue != null && !RefValue.IsEmpty; }
        }

        public override string GetShowText(Tuple<object, Summary> row)
        {
            Reference reference = (Reference)GetField(row);
            if (reference == null || reference.IsEmpty)
                return "";

            return reference.Summary.Title;

            //            var xParam = Expression.Parameter(typeof(T), "x");
            //var accessRef = Expression.MakeMemberAccess(xParam, this.PropInfo);
            //MethodInfo getSummMethod = RefValue.GetType().GetMethod("GetSummary").MakeGenericMethod(refType);
            //var getSumm = Expression.Call(accessRef, getSummMethod);
            //var accessProp = Expression.MakeMemberAccess(getSumm, refType.GetProperty(refFieldName));
            //var comp = Expression.Equal(accessProp, Expression.)
        }

        public override void MergeOriginal(ListFilter filt)
        {
            base.MergeOriginal(filt);

            var refFilt = (ReferenceFilter)filt;
            this.refFieldName = refFilt.refFieldName;
            this.refType = refFilt.refType;

            // This deals with the case where we are storing extra data (e.g. version) on the serializedvalue of the reference created by model binding
            // and we want to use this to update e.g. a CrossPartitionReference field stored in the RefValue of the original filter.  The Reference
            // type will store serialized data it doesn't know about when setting the SerializedValue property and return it when getting that property
            // to enable this behaviour.
            string serValue = this.RefValue.SerializedValue;
            this.RefValue = refFilt.RefValue;
            this.RefValue.SerializedValue = serValue;
        }
    }
}
