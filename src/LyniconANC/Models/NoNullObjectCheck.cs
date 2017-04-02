using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Utility;
using Newtonsoft.Json;

namespace Lynicon.Models
{
    /// <summary>
    /// A content visitor which determines whether when a type is constructed, whether it has
    /// any null-valued complex fields (which is against the rules for a content type)
    /// </summary>
    public class NoNullObjectCheck : ContentVisitor
    {
        /// <summary>
        /// The property which contains a null value when the type is constructed
        /// </summary>
        public PropertyInfo NullProperty { get; set; }

        public NoNullObjectCheck() : base()
        {
            this.PropertyFilter = pi => pi.CanRead
                && pi.CanWrite
                && pi.GetCustomAttribute<NotMappedAttribute>() == null
                && pi.GetCustomAttribute<JsonIgnoreAttribute>() == null;
        }

        /// <summary>
        /// Run a check on a new object of a given type to find any null-valued complex fields
        /// </summary>
        /// <param name="t">The type</param>
        /// <returns>The propertyinfo of a complex field initialised to null, or null if none exists</returns>
        public PropertyInfo Run(Type t)
        {
            if (t == null || t.GetConstructor(Type.EmptyTypes) == null)
                return null;

            object o = Activator.CreateInstance(t);
            NullProperty = null;
            Visit(o);
            return NullProperty;
        }

        public override void Object(System.Reflection.PropertyInfo pi, object val)
        {
            if (val == null)
                NullProperty = pi;
            base.Object(pi, val);
        }

        public override void List(System.Reflection.PropertyInfo pi, System.Collections.IList val)
        {
            if (val == null)
                NullProperty = pi;
            else
            {
                NullProperty = new NoNullObjectCheck().Run(ReflectionX.ElementType(val)) ?? NullProperty;
            }   
        }
    }
}
