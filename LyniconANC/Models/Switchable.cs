using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.ComponentModel;

namespace Lynicon.Models
{
    /// <summary>
    /// Switchable is used to allow for creation of content subtypes which can contain one of a range of possible other content subtypes.
    /// The editor UI manages Switchables by presenting the user with a switch between these possibilities which changes the editor
    /// for the Switchable property to that for the appropriate contained type
    /// </summary>
    [Serializable]
    public abstract class Switchable
    {
        private string selectedProperty = null;
        /// <summary>
        /// The property currently selected as the value of the Switchable
        /// </summary>
        public string SelectedProperty
        {
            get
            {
                if (selectedProperty == null)
                {
                    var props = this.GetType().GetProperties();
                    var prop = props.FirstOrDefault(p => p.GetCustomAttribute<DefaultPropertyAttribute>() != null)
                        ?? props.First();
                    return prop.Name;
                }
                return selectedProperty;
            }
            set { selectedProperty = value; }
        }

        /// <summary>
        /// The value of the switchable as a given content subtype
        /// </summary>
        /// <typeparam name="T">The type of the content subtype</typeparam>
        /// <returns>The value of that content subtype, or null if it's not currently selected</returns>
        public T Value<T>()
        {
            return (T)this.GetType().GetProperty(this.SelectedProperty).GetValue(this);
        }

        /// <summary>
        /// Set the value of the currently selected content subtype
        /// </summary>
        /// <param name="o">New value for the content subtype property currently selected</param>
        public void SetValue(object o)
        {
            this.GetType().GetProperty(this.SelectedProperty).SetValue(this, o);
        }
    }
}
