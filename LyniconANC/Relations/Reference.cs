using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Models;
using Lynicon.Repositories;
using Lynicon.Utility;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lynicon.Relations
{
    /// <summary>
    /// A content subtype to represent an un-typed reference to another content item
    /// </summary>
    [Serializable]
    public class Reference
    {
        public static Func<ItemVersionedId, Type, string, IEnumerable<Summary>> ReferenceGetter { get; set; }

        public static IEnumerable<Summary> GetReferencesFrom<TContent>(object o, string propertyName)
            where TContent : class
        {
            if (o is IContentContainer)
                return GetReferencesFrom<TContent>(new ItemVersionedId(o), propertyName);

            var cont = Collator.Instance.GetContainer(o);
            return GetReferencesFrom<TContent>(cont, propertyName);
        }
        public static IEnumerable<Summary> GetReferencesFrom<TContent>(ItemVersionedId ividToItem, string propertyName)
            where TContent : class
        {
            if (ReferenceGetter == null)
            {
                // constructs expression for x => x.propertyName != null && x.propertyName.VersionedId == ividToItem

                var xParam = Expression.Parameter(typeof(TContent), "x");
                var refProp = typeof(TContent).GetProperty(propertyName);
                var xGetRef = Expression.MakeMemberAccess(xParam, refProp);
                var neNull = Expression.NotEqual(xGetRef, Expression.Constant(null));
                if (!typeof(Reference).IsAssignableFrom(refProp.PropertyType))
                    throw new ArgumentException(string.Format("Trying to get references for type {0} property {1} which is not of Reference type", typeof(TContent).FullName, propertyName));
                var xGetIvid = Expression.MakeMemberAccess(xGetRef, refProp.PropertyType.GetProperty("VersionedId"));
                var ividConst = Expression.Constant(ividToItem);
                var comp = Expression.Equal(xGetIvid, ividConst);
                var test = Expression.AndAlso(neNull, comp);
                var lambda = Expression.Lambda<Func<TContent, bool>>(test, xParam);

                foreach (var item in Collator.Instance.Get<TContent, TContent>(iq => iq.Where(lambda)))
                    yield return Collator.Instance.GetSummary<Summary>(item);
            }
            else
                foreach (var summ in ReferenceGetter(ividToItem, typeof(TContent), propertyName))
                    yield return summ;
        }

        static Reference()
        {
            ReferenceGetter = null;
        }
        /// <summary>
        /// Cross-version identity as a string
        /// </summary>
        public string Id { get; set; }

        protected string extraSerializedData = null;

        /// <summary>
        /// The value of the reference serialized to a string
        /// </summary>
        [JsonIgnore, ScaffoldColumn(false)]
        public virtual string SerializedValue
        {
            get
            {
                return ItemId == null ? "" : ItemId.ToString() + (string.IsNullOrEmpty(extraSerializedData) ? "" : " " + extraSerializedData);
            }
            set
            {

                string strippedValue = (value ?? "").UpTo(" ");
                extraSerializedData = (value ?? "").After(" ");
                var itemId = new ItemId(strippedValue);
                if (itemId != null && itemId.Id != null)
                {
                    this.Id = itemId.Id.ToString();
                    this.DataType = itemId.Type.FullName;
                    this.summary = null; // force reload of summary
                }
                else
                {
                    this.Id = null;
                    this.DataType = null;
                    this.summary = null;
                }
            }
        }

        /// <summary>
        /// The name of the data type of the content item being referenced
        /// </summary>
        public virtual string DataType { get; set; }

        /// <summary>
        /// The data type of the content item being referenced
        /// </summary>
        [JsonIgnore, ScaffoldColumn(false)]
        public Type Type
        {
            get
            {
                if (string.IsNullOrEmpty(DataType))
                    return null;
                else
                    return ContentTypeHierarchy.GetContentType(DataType);
            }
        }

        /// <summary>
        /// The ItemId of the content item being referenced
        /// </summary>
        [JsonIgnore, ScaffoldColumn(false)]
        public ItemId ItemId
        {
            get
            {
                if (Type == null || string.IsNullOrEmpty(Id))
                    return null;
                return new ItemId(Id + ":" + Type.FullName);
            }
        }

        [JsonIgnore, ScaffoldColumn(false)]
        public virtual ItemVersionedId VersionedId
        {
            get
            {
                if (IsEmpty)
                    return null;
                return new ItemVersionedId(this.ItemId, VersionManager.Instance.CurrentVersion);
            }
            set
            {
                this.Id = value.Id.ToString();
                this.DataType = value.Type.FullName;
            }
        }

        /// <summary>
        /// Whether the reference can only refer to content items of one type
        /// </summary>
        public virtual bool FixedDataType
        {
            get { return false; }
        }

        /// <summary>
        /// Create a new empty reference
        /// </summary>
        public Reference()
        { }
        /// <summary>
        /// Create a new reference with a given data type and identity
        /// </summary>
        /// <param name="dataType">name of the referred to data type</param>
        /// <param name="id">Identity of the referred to item</param>
        public Reference(string dataType, string id)
        {
            this.DataType = dataType;
            this.Id = id;
        }
        /// <summary>
        /// Create a new reference for a given ItemId
        /// </summary>
        /// <param name="itemId">The ItemId</param>
        public Reference(ItemId itemId)
        {
            this.DataType = itemId.Type.FullName;
            this.Id = itemId.Id.ToString();
        }

        protected Summary summary = null;

        /// <summary>
        /// The summary of the referred to item
        /// </summary>
        [JsonIgnore, ScaffoldColumn(false)]
        public virtual Summary Summary
        {
            get
            {
                if (Id == null || string.IsNullOrEmpty(DataType))
                    return null;
                if (summary == null && ItemId != null)
                    summary = Collator.Instance.Get<Summary>(ItemId);
                return summary;
            }
        }

        /// <summary>
        /// Get the summary of the referred to item cast to a given summary type
        /// </summary>
        /// <typeparam name="T">Summary type to get</typeparam>
        /// <returns>Summary of type T</returns>
        public virtual T GetSummary<T>()
            where T : Summary
        {
            return Summary as T;
        }

        /// <summary>
        /// Get a select list for choosing items that can be held in the reference
        /// </summary>
        /// <returns>A list of SelectListItems for using to create a list for the user</returns>
        public virtual List<SelectListItem> GetSelectList()
        {
            return null;
        }

        /// <summary>
        /// An Id which indicates what is in the select list
        /// </summary>
        /// <returns>A string identifying the select list</returns>
        public virtual string SelectListId()
        {
            return null;
        }

        /// <summary>
        /// Test for whether the reference points to anything
        /// </summary>
        [JsonIgnore, ScaffoldColumn(false)]
        public virtual bool IsEmpty
        {
            get
            {
                if (string.IsNullOrEmpty(Id) || Type == null || Summary == null)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Shows the title of the referred to item if there is one
        /// </summary>
        /// <returns>The title of the referred to item if there is one</returns>
        public override string ToString()
        {
            if (IsEmpty)
                return "";
            else
                return Summary.Title;
        }
    }

    /// <summary>
    /// A typed reference which can only refer to content types which can be assigned to a given type
    /// </summary>
    /// <typeparam name="T">A type to which the content type of the referred to item must be assignable</typeparam>
    [Serializable]
    public class Reference<T> : Reference where T : class
    {
        /// <summary>
        /// Whether type T is a content type
        /// </summary>
        public static bool IsContentType { get; set; }

        /// <summary>
        /// List of the content types which can be assigned to T
        /// </summary>
        public static List<Type> AssignableContentTypes { get; set; }

        private static List<string> assignableContentTypeNames { get; set; }

        static Reference()
        {
            if (typeof(Summary).IsAssignableFrom(typeof(T)))
                throw new ArgumentException("Cannot make a reference<T> where T is a summary type " + typeof(T).FullName);

            IsContentType = ContentTypeHierarchy.AllContentTypes.Contains(typeof(T));
            if (IsContentType)
                AssignableContentTypes = new List<Type> { typeof(T) };
            else
                AssignableContentTypes = ContentTypeHierarchy.GetAssignableContentTypes(typeof(T));
            assignableContentTypeNames = AssignableContentTypes.Select(t => t.FullName).ToList();
        }

        private string dataType = null;
        public override string DataType
        {
            get { return IsContentType ? typeof(T).FullName : dataType; }
            set
            {
                if (!assignableContentTypeNames.Contains(value))
                    throw new ArgumentException("Reference type " + typeof(T).FullName + " cannot have a data type of " + value);
                dataType = value;
            }
        }

        public override bool FixedDataType
        {
            get
            {
                return IsContentType;
            }
        }

        public override string SerializedValue
        {
            get
            {
                if (IsContentType)
                    return Id + ":";
                else
                    return base.SerializedValue;
            }
            set
            {
                if (IsContentType && (value ?? "").Contains(":"))
                {
                    Id = (value ?? "").UpTo(":");
                    this.summary = null;
                } 
                else
                    base.SerializedValue = value ?? "";
            }
        }

        public Reference() : base() { }
        public Reference(string dataType, string id) : base(dataType, id)
        { }
        public Reference(ItemId itemId) : base(itemId)
        { }
        public Reference(string id)
        {
            this.Id = id;
        }

        public override string SelectListId()
        {
            return AssignableContentTypes.Select(t => t.Name).Join("_");
        }
        public override List<SelectListItem> GetSelectList()
        {

            var summaries = Collator.Instance.Get<Summary, object>(AssignableContentTypes, iq => iq);
            var slis = summaries.Select(s => new SelectListItem
            {
                Text = IsContentType ? s.Title : s.Title + " (" + BaseContent.ContentClassDisplayName(s.Type) + ")",
                Value = IsContentType ? s.Id.ToString() + ":" : s.ItemId.ToString(),
                Selected = (s.Id.ToString() == this.Id && s.Type == this.Type)
            }).OrderBy(sli => sli.Text).ToList();
            bool noneSelected = !slis.Any(sli => sli.Selected);
            slis.Insert(0, new SelectListItem { Text = "", Value = "", Selected = noneSelected });
            return slis;
        }

        // Necessary to make override in CrossVersionReference<T> : Reference<T> work, for some reason
        public override Summary Summary
        {
            get
            {
                return base.Summary;
            }
        }
    }
}
