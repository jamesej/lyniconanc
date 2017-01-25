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
using System.ComponentModel;

namespace Lynicon.Extensions
{
    /// <summary>
    /// A filter to use for filtering (or displaying) by a foreign key value
    /// </summary>
    public class ForeignKeyFilter : FieldFilter
    {
        private string refFieldName;
        private Type refType;

        public ForeignKeyFilter()
        { }
        public ForeignKeyFilter(FieldFilterAttribute filterAttr, PropertyInfo propInfo)
            : base(filterAttr, propInfo)
        {
            refFieldName = filterAttr.ReferencedFieldName;
            refType = filterAttr.ReferencedType;
        }
        public ForeignKeyFilter(string name, Type referencedType, string referencedFieldName, PropertyInfo propInfo)
            : base(name, propInfo)
        {
            refFieldName = referencedFieldName;
            refType = referencedType;
        }

        /// <summary>
        /// UI search field for the foreign key value
        /// </summary>
        public string Search { get; set; }

        private List<object> keySet = null;

        private void SetKeySet<T, TSumm>()
            where T : class
            where TSumm : Summary
        {
            // encodes x => (x.Prop ?? "").ToLower().Contains(srch)
            var srch = Search.ToLower();
            var testProp = typeof(T).GetProperty(this.refFieldName);
            var xParam = Expression.Parameter(typeof(T), "x");
            var accessProp = Expression.MakeMemberAccess(xParam, testProp);
            var coalescedString = Expression.Coalesce(accessProp, Expression.Constant(""));
            var toLowerMethod = typeof(string).GetMethod("ToLower", new Type[0]);
            var toLower = Expression.Call(coalescedString, toLowerMethod);
            var containsMethod = typeof(string).GetMethod("Contains");
            var srchParam = Expression.Constant(srch);
            var contains = Expression.Call(toLower, containsMethod, srchParam);
            var test = Expression.Lambda<Func<T, bool>>(contains, xParam);

            keySet = Collator.Instance.Get<TSumm, T>(new Type[] { typeof(T) }, iq => iq.Where(test))
                .Select(s => s.Id).ToList();  
        }

        public override Func<IQueryable<T>, IQueryable<T>> Apply<T>()
        {
            if (keySet == null)
            {
                Type summType = ContentTypeHierarchy.SummaryTypes[this.refType];
                ReflectionX.InvokeGenericMethod(this, "SetKeySet", false, mi => true, new List<Type> { this.refType, summType });
            }

            // encodes x => x.Prop
            var xParam = Expression.Parameter(typeof(T), "x");
            var accessProp = Expression.MakeMemberAccess(xParam, this.PropInfo);
            var selector = Expression.Lambda(accessProp, xParam);

            Type propType = this.PropInfo.PropertyType;

            Dictionary<Type, TypeConverter> converters =
                this.keySet.Select(k => k.GetType()).Distinct().ToDictionary(t => t, t => TypeDescriptor.GetConverter(t));

            this.keySet = this.keySet.Select(k => k.GetType() == propType
                ? k
                : converters[k.GetType()].ConvertTo(k, propType)).ToList();
            // if foreign key field was nullable
            return iq => iq.WhereIn(selector, this.keySet, propType);
        }

        public override bool Active
        {
            get { return !string.IsNullOrEmpty(Search); }
        }

        public override string GetShowText(Tuple<object, Summary> row)
        {
            object id = GetField(row);
            if (id == null)
                return "";

            ItemId iId = new ItemId(refType, id);
            var summ = Collator.Instance.Get<Summary>(iId);
            return summ == null ? "" : summ.DisplayTitle();
        }

        public override void MergeOriginal(ListFilter filt)
        {
            base.MergeOriginal(filt);

            var fkFilt = (ForeignKeyFilter)filt;
            this.refFieldName = fkFilt.refFieldName;
            this.refType = fkFilt.refType;
        }
    }
}
