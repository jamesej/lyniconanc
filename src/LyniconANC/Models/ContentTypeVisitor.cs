using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Collation;
using Newtonsoft.Json;
using Lynicon.Utility;

namespace Lynicon.Models
{
    public enum PropertyCategory
    {
        List, Primitive, Object, Unknown
    }

    public class ContentTypeVisitor
    {
        public static PropertyCategory GetTypeCategory(Type t)
        {
            if (t.GetInterface("IList") != null && t.IsGenericType())
            {
                return PropertyCategory.List;
            }
            else if (t.IsPrimitive() || t == typeof(string) || t == typeof(Guid) || t == typeof(DateTime) || typeof(ItemId).IsAssignableFrom(t))
            {
                return PropertyCategory.Primitive;
            }
            else if (t.IsClass())
            {
                return PropertyCategory.Object;
            }

            return PropertyCategory.Unknown;
        }

        public static IEnumerable<PropertyInfo> GetVisitableProperties(Type t)
        {
            foreach (var prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty))
            {
                if (prop.GetIndexParameters().Length > 0)
                    continue;

                yield return prop;
            }
        }
        public Func<PropertyInfo, bool> IncludeProperty { get; set; }

        public ContentTypeVisitor()
        {
            IncludeProperty = pi => true;
        }

        public virtual void Visit(PropertyInfo pi)
        {
            var propCat = GetTypeCategory(pi.PropertyType);
            switch (propCat)
            {
                case PropertyCategory.List:
                    List(pi);
                    break;
                case PropertyCategory.Primitive:
                    Primitive(pi);
                    break;
                case PropertyCategory.Object:
                    Object(pi);
                    break;
            }
        }
        public virtual void Visit(Type t)
        {
            foreach (var prop in GetVisitableProperties(t).Where(IncludeProperty))
            {
                Visit(prop);
            }
        }

        public virtual void List(PropertyInfo pi)
        {
            Visit(pi.PropertyType.GetGenericArguments().First());
        }

        public virtual void Primitive(PropertyInfo pi)
        {

        }

        public virtual void Object(PropertyInfo pi)
        {
            Visit(pi.PropertyType);
        }
    }
}
