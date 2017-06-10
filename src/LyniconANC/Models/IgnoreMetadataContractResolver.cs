using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using LyniconANC.Extensibility;
using System.Linq;

namespace Lynicon.Models
{
    public class IgnoreMetadataContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);

            var baseType = TypeExtender.BaseType(type);

            if (type == baseType)
                return properties;
            else
            {
                var baseTypeProps = baseType.GetProperties().Select(pi => pi.Name).ToList();
                return properties.Where(p => baseTypeProps.Contains(p.PropertyName)).ToList();
            }
        }
    }
}
