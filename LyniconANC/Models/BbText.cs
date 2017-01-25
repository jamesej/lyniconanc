using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cyotek.Web.BbCodeFormatter;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Globalization;
using System.Web;

namespace Lynicon.Models
{
    /// <summary>
    /// A converter that simply serializes a BbText as its text in JSON
    /// </summary>
    public class BbTextJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((value as BbText).Text);
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(BbText).IsAssignableFrom(objectType);
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }
        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
                return (BbText)null;
            BbText bbt = Activator.CreateInstance(objectType, reader.Value as string) as BbText;
            return bbt;
        }
    }

    /// <summary>
    /// A TypeConverter for BbText to and from string
    /// </summary>
    public class BbTextConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context,
           Type sourceType)
        {

            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context,
           CultureInfo culture, object value)
        {
            if (value is string)
            {
                return new BbText(value as string);
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context,
           CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return (value as BbText).Text;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    /// <summary>
    /// BbText type, stores BbText and converts it automatically to HTML when it is rendered in a page
    /// </summary>
    [JsonConverter(typeof(BbTextJsonConverter)), TypeConverter(typeof(BbTextConverter)), Serializable]
    public class BbText : IHtmlString
    {
        public static implicit operator BbText(string s)
        {
            if (s == null)
                return (BbText)null;
            return new BbText(s);
        }
        public static implicit operator string(BbText t)
        {
            if (t == null) return (string)null;
            return t.ToString();
        }

        /// <summary>
        /// Constant for an empty BbText
        /// </summary>
        public static BbText Empty
        {
            get
            {
                return new BbText("");
            }
        }

        string text = null;

        /// <summary>
        /// The actual BbText
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Create an empty BbText
        /// </summary>
        public BbText()
        { }
        /// <summary>
        /// Create a BbText from the BbText as a string
        /// </summary>
        /// <param name="s">The BbText format</param>
        public BbText(string s)
        {
            Text = s;
            text = BbCodeProcessor.Format(s);
        }

        /// <summary>
        /// This returns the HTML of the BbText so in the view, this is what is output
        /// </summary>
        /// <returns>HTML of BbText</returns>
        public override string ToString()
        {
            return text;
        }

        #region IHtmlString Members

        /// <summary>
        /// The returns the HTML of the BbText
        /// </summary>
        /// <returns>The HTML of the BbText</returns>
        public string ToHtmlString()
        {
            return text;
        }

        #endregion
    }
}
