using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Models
{
    /// <summary>
    /// Prepares content for binding to its edited version.  Model binding will not create an empty
    /// list, so lists are cleared then reloaded with the edited versions.
    /// </summary>
    public class ContentBindingPreparer : ContentVisitor
    {
        public override void List(PropertyInfo pi, IList val)
        {
            if (val != null)
                val.Clear();
        }
    }
}
