using System;

namespace Lynicon.Attributes
{
    /// <summary>
    /// Mark a property (which must be on a container class or summary class) as requring a filter to be created
    /// to allow filtering by or showing that property on the filters UI
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FieldFilterAttribute : Attribute
    {
        /// <summary>
        /// Name of the filter as displayed
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Whether the filter will have a 'show' checkbox to show the value in the output
        /// </summary>
        public bool Showable { get; set; }

        /// <summary>
        /// Allows for filtering by a property on an object to which this property is a reference:  The type of the referenced object
        /// </summary>
        public Type ReferencedType { get; set; }
        /// <summary>
        /// Allows for filtering by a property on an object to which this property is a reference:  The name of the property of the referenced object
        /// to use for filtering or to display
        /// </summary>
        public string ReferencedFieldName { get; set; }

        public FieldFilterAttribute()
        {
            Name = null;
            ReferencedType = null;
            Showable = true;
        }
        public FieldFilterAttribute(string name) : this()
        {
            Name = name;
        }
        
    }
}
