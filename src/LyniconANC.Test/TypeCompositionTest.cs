using Lynicon.Extensibility;
using Lynicon.Membership;
using Lynicon.Repositories;
using LyniconANC.Test.Models;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LyniconANC.Test
{
    [TestFixture]
    public class TypeCompositionTest
    {
        [Test]
        public void ComposedTypesSerialize()
        {
            var dict = new Dictionary<Type, string>();
            //Type extUserType = CompositeTypeManager.Instance.ExtendedTypes[typeof(TestData)];
            dict.Add(typeof(TestData), "aaa");
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            string serialized = JsonConvert.SerializeObject(dict, settings);
            var res = JsonConvert.DeserializeObject<Dictionary<Type, string>>(serialized, settings);

            Assert.AreEqual("aaa", res[typeof(TestData)]);
        }
    }
}
