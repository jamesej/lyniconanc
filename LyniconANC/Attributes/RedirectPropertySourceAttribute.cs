using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lynicon.Collation;

namespace Lynicon.Attributes
{
    /// <summary>
    /// This attribute is used in content types to instruct the collator to automatically collate data from a
    /// source record into the content item when reading and to distribute changes back to that
    /// sources when writing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class RedirectPropertySourceAttribute : Attribute
    {
        /// <summary>
        /// This string indicates a list of properties which are fetched from the external source.
        /// The properties can include '.' separators to indicate a property path.
        /// The syntax is classpropertypath [> sourcepropertypath], classpropertyname,...
        /// The sourcepropertypath is only needed if it is different from the classpropertypath.
        /// </summary>
        public string PropertyPath { get; set; }
        /// <summary>
        /// The source descriptor is a C# format string which generates an address path from another address path by replacing
        /// {0}, {1} etc in the SourceDescriptor with path elements 0, 1 etc from the current address path.
        /// </summary>
        public string SourceDescriptor { get; set; }
        /// <summary>
        /// If true, the properties indicated by the PropertyPath are not written back to the source records
        /// on a save
        /// </summary>
        public bool ReadOnly { get; set; }
        /// <summary>
        /// The content type of the external source record
        /// </summary>
        public Type ContentType { get; set; }

        protected Guid UniqueId { get; set; }

        /// <summary>
        /// List of property paths as enumerable
        /// </summary>
        public IEnumerable<string> PropertyPaths
        {
            get { return PropertyPath.Split(',').Select(p => p.Trim()); }
        }

        public RedirectPropertySourceAttribute(string propertyPath)
        {
            PropertyPath = propertyPath;
            SourceDescriptor = "";
            ReadOnly = false;
            UniqueId = Guid.NewGuid();
            ContentType = null;
        }

        public override object TypeId
        {
            get
            {
                return UniqueId;
            }
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(SourceDescriptor))
                return "{redirect " + PropertyPath + " on default content address}";
            else
                return "{redirect " + PropertyPath + " on " + SourceDescriptor + "}";
        }
    }
}
