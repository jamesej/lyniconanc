using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Relations;

namespace Lynicon.Models
{
    /// <summary>
    /// Checks that all references in a content item point to something valid
    /// </summary>
    public class ContentReferenceChecker : ContentVisitor
    {
        /// <summary>
        /// Set to the title of the content item after Visiting it
        /// </summary>
        public string ItemTitle { get; set; }
        /// <summary>
        /// List of all reference errors in the content item
        /// </summary>
        public List<string> Errors { get; set; }

        public ContentReferenceChecker() : base()
        {
            Errors = new List<string>();
        }

        public override void Visit(PropertyInfo pi, object val)
        {
            base.Visit(pi, val);
        }

        public override void Object(PropertyInfo pi, object val)
        {
            if (typeof(Reference).IsAssignableFrom(pi == null ? val.GetType() : pi.PropertyType))
            {
                Reference r = (Reference)val;
                if (r.ItemId != null && r.Summary == null)
                    Errors.Add(ItemTitle + " - " + pi.Name + " to " + r.ItemId.Type.Name + ": " + r.ItemId.Id.ToString());
            }
            else
                base.Object(pi, val);
        }
    }
}
