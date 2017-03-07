using Lynicon.Collation;
using Lynicon.Relations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Lynicon.Extensibility
{
    public class LyniconIdentifierTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(ItemId).IsAssignableFrom(objectType)
                || typeof(ItemVersion).IsAssignableFrom(objectType)
                || typeof(Reference).IsAssignableFrom(objectType)
                || typeof(Address).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //string s = reader.ReadAsString();
            object o = reader.Value;
            object val = Activator.CreateInstance(objectType, o.ToString());
            return val;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
