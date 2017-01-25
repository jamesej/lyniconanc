using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Collation;
using Newtonsoft.Json;

namespace Lynicon.Models
{
    /// <summary>
    /// A utility class which visits the properties and subproperties of a content type, to
    /// be overridden to provide functionality that needs to do this search
    /// </summary>
    public class ContentVisitor
    {
        /// <summary>
        /// Ignore the property if the function on the property info is false
        /// </summary>
        public Func<PropertyInfo, bool> PropertyFilter { get; set; }

        public ContentVisitor()
        {
            PropertyFilter = pi => true;
        }

        /// <summary>
        /// Visit a content object
        /// </summary>
        /// <typeparam name="T">The type of the content</typeparam>
        /// <param name="content">the content object to visit</param>
        public void Visit<T>(T content) where T : class
        {
            Visit(null, content);
        }
        /// <summary>
        /// Visit a content object
        /// </summary>
        /// <param name="content">The content object to visit</param>
        public void Visit(object content)
        {
            Visit(null, content);
        }

        /// <summary>
        /// Action to take when visiting a single property
        /// </summary>
        /// <param name="pi">The property info of the property</param>
        /// <param name="val">The value of the property</param>
        public virtual void Visit(PropertyInfo pi, object val)
        {
            if (pi == null && val == null)
                return;

            Type t = pi == null ? val.GetType() : pi.PropertyType;
            if (t.GetInterface("IList") != null)
            {
                List(pi, (IList)val);
            }
            else if (t.IsPrimitive || t == typeof(string) || t == typeof(DateTime) || typeof(ItemId).IsAssignableFrom(t))
            {
                Primitive(pi, val);
            }
            else if (t.IsClass
                     && (pi == null || pi.GetIndexParameters().Length == 0))
            {
                Object(pi, val);
            }

        }

        /// <summary>
        /// Action to take when visiting a primitive property
        /// </summary>
        /// <param name="pi">property info of the property</param>
        /// <param name="val">value of the property</param>
        public virtual void Primitive(PropertyInfo pi, object val)
        {
        }

        /// <summary>
        /// Action to take when visiting a compound/complex property
        /// </summary>
        /// <param name="pi">property info of the property</param>
        /// <param name="val">value of the property</param>
        public virtual void Object(PropertyInfo pi, object val)
        {
            if (val == null)
                return;

            Type t = val.GetType();
            foreach (var prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty))
            {
                if (prop.GetIndexParameters().Length > 0)
                    continue;
                if (!PropertyFilter(prop))
                    continue;
                ScaffoldColumnAttribute sca = prop.GetCustomAttribute<ScaffoldColumnAttribute>();
                if (sca != null && !sca.Scaffold)
                    continue;
                JsonIgnoreAttribute jia = prop.GetCustomAttribute<JsonIgnoreAttribute>();
                if (jia != null)
                    continue;

                Visit(prop, val == null ? null : prop.GetValue(val));
            }
        }

        /// <summary>
        /// Action to take when visiting a list property
        /// </summary>
        /// <param name="pi">The property info of the property</param>
        /// <param name="val">The value of the property</param>
        public virtual void List(PropertyInfo pi, IList val)
        {

        }
    }
}
