using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cyotek.Web.BbCodeFormatter;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Globalization;
using Lynicon.Utility;
using Microsoft.AspNetCore.Html;
using System.Runtime.Serialization;

namespace Lynicon.Models
{
    /// <summary>
    /// A converter that serializes a MinHtml to/from its html as JSON
    /// </summary>
    public class HtmlJsonConverter<THtml> : JsonConverter
        where THtml : HtmlString
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((value as THtml).Value);
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(THtml).IsAssignableFrom(objectType);
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
                return (THtml)null;
            // we assume the MinHtml is always clean when serialized
            THtml mHtml = Activator.CreateInstance(objectType, reader.Value as string, true) as THtml;
            return mHtml;
        }
    }

    /// <summary>
    /// A converter that converts a string with HTML to and from a MinHtml type
    /// </summary>
    public class HtmlConverter<THtml> : TypeConverter
        where THtml : HtmlString, new()
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
                var res = Activator.CreateInstance(typeof(THtml), value);
                return res;
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context,
           CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return (value as MinHtml).Value;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    /// <summary>
    /// Type which contains a very reduced subset of html consisting of basic headings, font formatting and links
    /// </summary>
    [JsonConverter(typeof(HtmlJsonConverter<MinHtml>)), TypeConverter(typeof(HtmlConverter<MinHtml>)), Serializable]
    public class MinHtml : HtmlString, ISerializable
    {
        public static implicit operator MinHtml(string s)
        {
            if (s == null)
                return (MinHtml)null;
            return new MinHtml(s);
        }
        public static implicit operator string(MinHtml t)
        {
            if (t == null) return (string)null;
            return t.ToString();
        }

        /// <summary>
        /// Constant for an empty MinHtml
        /// </summary>
        public static MinHtml Empty
        {
            get
            {
                return new MinHtml("");
            }
        }

        /// <summary>
        /// Create an empty MinHtml
        /// </summary>
        public MinHtml() : base("")
        { }
        /// <summary>
        /// Create a MinHtml from an HTML string (which is cleaned of illegal tags)
        /// </summary>
        /// <param name="anyHtml">HTML string</param>
        public MinHtml(string anyHtml) : base(HtmlX.MinimalHtml(anyHtml, true))
        {
        }
        /// <summary>
        /// Create a MinHtml from an HTML string with optional cleaning
        /// </summary>
        /// <param name="anyHtml">HTML string</param>
        /// <param name="isClean">If true, don't clean</param>
        public MinHtml(string anyHtml, bool isClean) : base(isClean ? anyHtml : HtmlX.MinimalHtml(anyHtml, true))
        {
        }

        protected MinHtml(SerializationInfo info, StreamingContext context) : base(info.GetString("Value"))
        {
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Value", this.Value);
        }
    }
    
    /// <summary>
    /// Subtype of min HTML not allowing links
    /// </summary>
    [Serializable]
    public class MinHtmlNoLinks : MinHtml
    {
        /// <summary>
        /// Create an empty MinHtmlNoLinks
        /// </summary>
        public MinHtmlNoLinks() : base("")
        { }
        /// <summary>
        /// Create a MinHtmlNoLinks from an HTML string (which is cleaned of illegal tags)
        /// </summary>
        /// <param name="anyHtml">HTML string</param>
        public MinHtmlNoLinks(string anyHtml) : base(anyHtml)
        {
        }
        /// <summary>
        /// Create a MinHtmlNoLinks from an HTML string with optional cleaning
        /// </summary>
        /// <param name="anyHtml">HTML string</param>
        /// <param name="isClean">If true, don't clean</param>
        public MinHtmlNoLinks(string anyHtml, bool isClean) : base(anyHtml, isClean)
        {
        }
    }

    [JsonConverter(typeof(HtmlJsonConverter<MedHtml>)), TypeConverter(typeof(HtmlConverter<MedHtml>)), Serializable]
    public class MedHtml : MinHtml
    {
        /// <summary>
        /// Create an empty MedHtml
        /// </summary>
        public MedHtml() : base("")
        { }
        /// <summary>
        /// Create a MedHtml from an HTML string (which is cleaned of illegal tags)
        /// </summary>
        /// <param name="anyHtml">HTML string</param>
        public MedHtml(string anyHtml) : base(anyHtml)
        {
        }
        /// <summary>
        /// Create a MedHtml from an HTML string with optional cleaning
        /// </summary>
        /// <param name="anyHtml">HTML string</param>
        /// <param name="isClean">If true, don't clean</param>
        public MedHtml(string anyHtml, bool isClean) : base(anyHtml, isClean)
        {
        }

        protected MedHtml(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [JsonConverter(typeof(HtmlJsonConverter<MaxHtml>)), TypeConverter(typeof(HtmlConverter<MaxHtml>)), Serializable]
    public class MaxHtml : MinHtml
    {
        /// <summary>
        /// Create an empty MaxHtml
        /// </summary>
        public MaxHtml() : base("")
        { }
        /// <summary>
        /// Create a MaxHtml from an HTML string (which is cleaned of illegal tags)
        /// </summary>
        /// <param name="anyHtml">HTML string</param>
        public MaxHtml(string anyHtml) : base(anyHtml, true)
        {
        }
        /// <summary>
        /// Create a MaxHtml from an HTML string with optional cleaning
        /// </summary>
        /// <param name="anyHtml">HTML string</param>
        /// <param name="isClean">If true, don't clean</param>
        public MaxHtml(string anyHtml, bool isClean) : base(anyHtml, true)
        {
        }

        protected MaxHtml(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
