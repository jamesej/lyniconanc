using Lynicon.Extensibility;
using Lynicon.Membership;
using Lynicon.Repositories;
using LyniconANC.Test.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace LyniconANC.Test
{
    public class TypeCompositionTest
    {
        [Fact]
        public void ComposedTypesSerialize()
        {
            var dict = new Dictionary<Type, string>();
            //Type extUserType = CompositeTypeManager.Instance.ExtendedTypes[typeof(TestData)];
            dict.Add(typeof(TestData), "aaa");
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            string serialized = JsonConvert.SerializeObject(dict, settings);
            var res = JsonConvert.DeserializeObject<Dictionary<Type, string>>(serialized, settings);

            Assert.Equal("aaa", res[typeof(TestData)]);
        }
    }
}
