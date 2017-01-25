using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Attributes;
using Lynicon.Models;
using Lynicon.Utility;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// Abstract type for all filters usable on the Filters UI page
    /// </summary>
    [UsePolymorphicBinding]
    public abstract class ListFilter
    {
        /// <summary>
        /// Used in creating a filter representing user options to copy
        /// values from the original not set by the user via model binding
        /// of input fields
        /// </summary>
        /// <param name="filt"></param>
        public abstract void MergeOriginal(ListFilter filt);

        /// <summary>
        /// Index for identifying the filter when its settings are
        /// submitted to the server
        /// </summary>
        public int Idx { get; set; }

        /// <summary>
        /// Whether the filter is active, i.e. if false the filter
        /// can be ignored
        /// </summary>
        public abstract bool Active { get; }

        /// <summary>
        /// Whether the value being filtered (if this is a value-based filter)
        /// should be shown in the output
        /// </summary>
        public bool Show { get; set; }

        /// <summary>
        /// 1: sort asc, 0: no sort, -1: sort desc
        /// </summary>
        [UIHint("Sorter")]
        public int Sort { get; set; }

        /// <summary>
        /// Get the text that should go onto a column of the output for this filter
        /// from a filter result item
        /// </summary>
        /// <param name="row">A filter result item (a summarised container and a Summary)</param>
        /// <returns>String to display</returns>
        public abstract string GetShowText(Tuple<object, Summary> row);

        /// <summary>
        /// Type of content item this filter can apply to
        /// </summary>
        public Type ApplicableType { get; set; }

        /// <summary>
        /// Name of the filter (its displayed title)
        /// </summary>
        public virtual string Name { get; set; }
        
        /// <summary>
        /// Apply this filter to an IQueryable
        /// </summary>
        /// <typeparam name="T">The item type of the IQueryable</typeparam>
        /// <returns>New IQueryable with this filter applied to it</returns>
        public abstract Func<IQueryable<T>, IQueryable<T>> Apply<T>();

        /// <summary>
        /// Apply an OrderBy to an enumerable of list result items to sort it according to this filter's
        /// settings
        /// </summary>
        /// <param name="source">The unsorted enumerable of list result items</param>
        /// <returns></returns>
        public virtual IEnumerable<Tuple<object, Summary>> ApplySort(IEnumerable<Tuple<object, Summary>> source)
        {
            return source;
        }

        /// <summary>
        /// The full type name of this filter so it can be recreated in model binding.  This is used by
        /// TypeDescriminatingModelBinder to create the right type of filter.
        /// </summary>
        public string ModelType
        {
            get { return this.GetType().AssemblyQualifiedName;  }
            set { }
        }

        /// <summary>
        /// List of headers for the values in GetShowText() when displayed
        /// </summary>
        /// <returns>List of headers</returns>
        public virtual List<string> Headers()
        {
            return new List<string> { this.Name };
        }

        /// <summary>
        /// Gets name of view for filter's settings editor by convention: this is in the area named after the project name
        /// of the project in which the filter subclass is declared, in Views/Items/EditorTemplates, the file named after the
        /// name of the filter class
        /// </summary>
        public virtual string ViewName
        {
            get
            {
                // default view location via namespace.  Note this is for EditorFor() calls, so it is a relative path with no suffix
                string filename = this.GetType().Name.UpTo("`");
                string ns = this.GetType().Namespace;
                if (!ns.StartsWith("Lynicon")) // client filter type, should be in main views folder of site
                    return filename;
                else
                {
                    string areaDir = ns.Replace(".Extensions", "").Replace(".Models", "");
                    // build the relative path off the shortest of the search paths ~/Views/Shared/EditorTemplates/{templateName}.cshtml
                    string dir = "../../../Areas/" + areaDir + "/Views/Items/EditorTemplates/";
                    return dir + filename;
                }
            }
        }
    }
}
