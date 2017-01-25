using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Models;
using Lynicon.Repositories;
using Lynicon.Utility;

namespace Lynicon.Collation
{
    /// <summary>
    /// Uniquely identifies a content item (which may have multiple versions)
    /// </summary>
    [Serializable]
    public class ItemId : IEquatable<ItemId>
    {
        /// <summary>
        /// Deserialize an ItemId from a string
        /// </summary>
        /// <param name="idStr">Serialized item id</param>
        /// <returns>The serialized ItemId</returns>
        public static object StringToId(string idStr)
        {
            if (string.IsNullOrEmpty(idStr))
                return null;

            int intId;
            Guid guid;
            if (Guid.TryParse(idStr, out guid))
                return guid;
            else if (int.TryParse(idStr, out intId))
                return intId;
            else if (idStr.StartsWith("'") && idStr.EndsWith("'"))
                return idStr.Substring(1, idStr.Length - 2);
            else
                return idStr;
        }

        private object id = null;
        /// <summary>
        /// Unversioned identifier of item, unique for the type of item
        /// </summary>
        public object Id
        {
            get { return id; }
            set
            {
                if (value == null)
                    throw new ArgumentException("ItemId's Id cannot be null");
                if (value is string)
                    id = StringToId((string)value);
                else
                    id = value;
            }
        }
        /// <summary>
        /// Type of the item
        /// </summary>
        public Type Type { get; set; }

        public ItemId()
        { }
        /// <summary>
        /// Construct an ItemId from a given type and identifier
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="id">The identifier</param>
        public ItemId(Type type, object id)
        {
            this.Id = id;
            this.Type = type == null ? null : type.ContentType();
        }
        public ItemId(ItemId iid) : this(iid.Type, iid.Id)
        { }
        /// <summary>
        /// Construct an ItemId by extracting it from a container
        /// </summary>
        /// <param name="container">The container</param>
        public ItemId(IContentContainer container)
            : this(Collator.GetContentType(container), Collator.Instance.GetIdProperty(container.GetType().UnproxiedType()).GetValue(container))
        {
        }
        /// <summary>
        /// Construct an ItemId from a summary
        /// </summary>
        /// <param name="summary">The summary</param>
        public ItemId(Summary summary)
        {
            this.Id = summary.Id;
            this.Type = summary.Type;
        }
        /// <summary>
        /// Constructs an ItemId from an unspecified object which can be a container, a content class or a summary
        /// </summary>
        /// <param name="o"></param>
        public ItemId(object o)
        {
            if (o is IContentContainer)
            {
                this.Type = Collator.GetContentType(o);
                this.Id = Collator.Instance.GetIdProperty(o.GetType().UnproxiedType()).GetValue(o);
            }
            else if (o is Summary)
            {
                this.Type = ((Summary)o).Type;
                this.Id = ((Summary)o).Id;
            }
            else
            {
                var container = Collator.Instance.GetContainer(o);
                this.Type = Collator.GetContentType(container);
                this.Id = Collator.Instance.GetIdProperty(container.GetType().UnproxiedType()).GetValue(container);
            }
        }
        /// <summary>
        /// Construct an ItemId by deserializing it from a string (created via .ToString())
        /// </summary>
        /// <param name="s">Serialized ItemId</param>
        public ItemId(string s)
        {
            if (string.IsNullOrEmpty(s) || !s.Contains(":"))
                return;

            string[] parts = s.Split(':');
            var idStr = parts[0].Trim();
            var typeStr = parts[1].Trim();
            if (string.IsNullOrEmpty(idStr) || string.IsNullOrEmpty(typeStr))
                return;
            this.Id = idStr;
            this.Type = ContentTypeHierarchy.GetContentType(typeStr);
        }

        /// <summary>
        /// Serializes the item id
        /// </summary>
        /// <returns>ItemId serialized to a string</returns>
        public override string ToString()
        {
            if (Id == null || Type == null)
                return null;
            else
                return this.Id.ToString() + ":" + this.Type.FullName;
        }

        // Define equality
#region equality

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ItemId);
        }

        public bool Equals(ItemId id)
        { 
            if (Object.ReferenceEquals(id, null))
                return false;

            if (Object.ReferenceEquals(this, id))
                return true;

            if (this.GetType() != id.GetType())
                return false;

            if (Id == null)
                return id.Id == null;
 
            return (Id.Equals(id.Id)) && (Type == id.Type);
        }

        public override int GetHashCode()
        {
            if (Id == null || Type == null)
                return 0;
            return Id.GetHashCode() * 0x00010000 + Type.GetHashCode();
        }

        public static bool operator ==(ItemId lhs, ItemId rhs)
        {
            // Check for null on left side. 
            if (Object.ReferenceEquals(lhs, null))
            {
                if (Object.ReferenceEquals(rhs, null))
                {
                    // null == null = true. 
                    return true;
                }

                // Only the left side is null. 
                return false;
            }
            // Equals handles case of null on right side. 
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ItemId lhs, ItemId rhs)
        {
            return !(lhs == rhs);
        }

#endregion
    }
}
