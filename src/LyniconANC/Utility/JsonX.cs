using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Lynicon.Utility
{
    public static class JsonX
    {
        /// <summary>
        /// Copy a property from another JObject, to a property path on this one, overwriting an existing property or creating a new one
        /// </summary>
        /// <param name="toObject">Object receiving property update</param>
        /// <param name="toPath">Path of property to update/add</param>
        /// <param name="fromObject">Object supplying property</param>
        /// <param name="fromPath">Path of property to supply</param>
        public static void CopyPropertyFrom(this JObject toObject, string toPath, JObject fromObject, string fromPath)
        {
            JProperty toProperty = toObject.PropertyByPath(toPath);
            JProperty fromProperty = fromObject.PropertyByPath(fromPath);
            if (fromProperty != null)
            {
                if (toProperty == null)
                    toObject.AddAtPath(toPath, fromProperty);
                else
                    toProperty.Replace(fromProperty);
            }
        }

        /// <summary>
        /// Get the JProperty at a specific property path
        /// </summary>
        /// <param name="jObject">the source object</param>
        /// <param name="path">the property path</param>
        /// <returns>the JPropery at the path</returns>
        public static JProperty PropertyByPath(this JObject jObject, string path)
        {
            string leafParent = path.Contains(".") ? path.UpToLast(".") : "";
            string leafProperty = path.Contains(".") ? path.LastAfter(".") : path;
            JObject parent = jObject;
            if (!string.IsNullOrEmpty(leafParent))
                parent = jObject.SelectToken(leafParent) as JObject;
            return parent == null ? null : parent.Property(leafProperty);
        }

        /// <summary>
        /// Add a new property to the JObject at a given property path
        /// </summary>
        /// <param name="jo">the source object</param>
        /// <param name="path">the property path</param>
        /// <param name="property">the property to add at the path</param>
        public static void AddAtPath(this JObject jo, string path, JProperty property)
        {
            JObject leafParent = jo;
            if (path.Contains("."))
            {
                string[] leafParents = path.UpToLast(".").Split('.');
                foreach (string propName in leafParents)
                {
                    JToken child = leafParent[propName];
                    if (child is JValue)
                    {
                        leafParent.Remove(propName);
                        child = null;
                    }
                    if (child == null)
                    {
                        child = new JObject();
                        leafParent.Add(propName, child);
                    }
                    leafParent = child as JObject;
                }
            }
            string leafProperty = path.Contains(".") ? path.LastAfter(".") : path;
            leafParent.Add(leafProperty, property.Value);
        }


    }
}
