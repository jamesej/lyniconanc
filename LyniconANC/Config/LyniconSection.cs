using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Lynicon.Config
{
    /// <summary>
    /// Object model for the Lynicon configuration
    /// </summary>
    public class LyniconSection : ConfigurationSection
    {
        /// <summary>
        /// The area base url
        /// </summary>
        [ConfigurationProperty("lyniconAreaBaseUrl")]
        public ValueElement LyniconAreaBaseUrl
        {
            get { return (ValueElement)this["lyniconAreaBaseUrl"]; }
            set { this["lyniconAreaBaseUrl"] = value; }
        }

        /// <summary>
        /// The root folder for the file manager
        /// </summary>
        [ConfigurationProperty("lyniconFileManagerRoot")]
        public ValueElement LyniconFileManagerRoot
        {
            get { return (ValueElement)this["lyniconFileManagerRoot"]; }
            set { this["lyniconFileManagerRoot"] = value; }
        }
    }

    public class ValueElement : ConfigurationElement
    {
        [ConfigurationProperty("value", IsRequired = true)]
        public string Value
        {
            get { return (string)this["value"]; }
            set { this["value"] = value; }
        }
    }
}
