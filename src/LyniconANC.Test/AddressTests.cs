using System;
using System.Collections.Generic;
using System.Linq;
using Lynicon.Attributes;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Models;
using Lynicon.Repositories;
using LyniconANC.Test.Models;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Xunit;

// Initialise database with test data
//  use ef directly, use appropriate schema for modules in use
// Attach event handlers to run at end of others, handlers store data for checking in class local vars
// 

namespace LyniconANC.Test
{
    [Collection("Lynicon System")]
    public class AddressTests
    {
        List<PathAddressData> paDatas = new List<PathAddressData> {
                new PathAddressData { P = "", X = "01" },
                new PathAddressData { P = "a", X = "02" },
                new PathAddressData { P = "a&b", X = "03" },
                new PathAddressData { P = "c", X = "04"}
            };
        List<SplitAddressData> paDatas2 = new List<SplitAddressData> {
                new SplitAddressData { A = "a", B = 1, C = "01" },
                new SplitAddressData { A = "b", B = 2, C = "02" },
                new SplitAddressData { A = "b", B = 3, C = "03" }
            };

        LyniconSystemFixture sys;

        public AddressTests(LyniconSystemFixture sys)
        {
            this.sys = sys;
        }

        [Fact]
        public void AddressEquality()
        {
            Dictionary<string, object> address = new Dictionary<string, object> { { "Existence", "Exists" }, { "Published", false } };
            var ii0 = new Address(typeof(HeaderContent), "abc&def");
            var ii1 = new Address(typeof(HeaderContent), "abc&def");
            var ii2 = new Address(typeof(RestaurantContent), "abc&def");
            var ii3 = new Address(typeof(HeaderContent), "abc");

            Assert.True(ii0.Equals(ii1), ".Equals true");
            Assert.True(ii0 == ii1, "== true");
            Assert.False(ii0.Equals(ii2), ".Equals false by type");
            Assert.False(ii0 == ii2, "== false by different val");
            Assert.False(ii1.Equals(ii3), ".Equals false by missing key");
            Assert.False(ii1 == ii3, "== false by missing key");

            Assert.False(ii0.GetHashCode() == ii2.GetHashCode(), "hash code by type");
            Assert.False(ii1.GetHashCode() == ii3.GetHashCode(), "hash code by missing key");
        }

        [Fact]
        public void AddressPathConstructor()
        {
            string path = null;
            var a1 = new Address(typeof(HeaderContent), path);
            Assert.Equal(0, a1.Count);

            var a2 = new Address(typeof(HeaderContent), "abc");
            Assert.Equal("abc", a2["_0"]);
            Assert.Equal(1, a2.Count);

            var a3 = new Address(typeof(HeaderContent), "p&qr");
            Assert.Equal("p", a3["_0"]);
            Assert.Equal("qr", a3["_1"]);
            Assert.Equal(2, a3.Count);

            var a4 = new Address(typeof(TestData), "abc&def");
            Assert.Equal("abc", a4["_0"]);
            Assert.Equal("def", a4["_1"]);
            Assert.Equal(2, a4.Count);
        }

        [Fact]
        public void AddressRouteDataConstructor()
        {
            var rd = new RouteData();
            var a1 = new Address(typeof(HeaderContent), rd);
            Assert.Equal("", a1.GetAsContentPath());

            rd.Values.Add("_0", "qqq");
            var a2 = new Address(typeof(HeaderContent), rd);
            Assert.Equal("qqq", a2.GetAsContentPath());

            rd.Values.Add("_1", "abc");
            var a3 = new Address(typeof(HeaderContent), rd);
            Assert.Equal("qqq&abc", a3.GetAsContentPath());
            Assert.NotEqual(a2.GetAsContentPath(), a3.GetAsContentPath());
        }

        [Fact]
        public void AddressObjectConstructor()
        {
            var a1 = new Address(paDatas2[0]);
            Assert.Equal("a", a1["_0"]);
            Assert.Equal(1, a1["_1"]);
            Assert.Equal("a&001", a1.GetAsContentPath());
            Assert.Equal("001", a1.GetAsString("_1"));

            var a2 = new Address(paDatas[2]);
            Assert.Equal("a", a2["_0"]);
            Assert.Equal("b", a2["_1"]);
            Assert.Equal("a&b", a2.GetAsContentPath());
        }

        [Fact]
        public void AddressSerialization()
        {
            var a1 = new Address("LyniconANC.Test.Models.HeaderContent:_0=a&_1=1");
            Assert.Equal("a", a1["_0"]);
            Assert.Equal("1", a1["_1"]);
            Assert.Equal("HeaderContent", a1.Type.Name);

            var a2dict = new Dictionary<string, object>();
            a2dict["_0"] = "a";
            a2dict["_1"] = 1;
            var a2 = new Address(typeof(HeaderContent), a2dict);
            Assert.Equal("LyniconANC.Test.Models.HeaderContent:_0=a&_1=1", a2.ToString());

            var dict = new Dictionary<Address, string>();
            dict.Add(a2, "hello");

            string ser = JsonConvert.SerializeObject(dict);
            var dictOut = JsonConvert.DeserializeObject<Dictionary<Address, string>>(ser);

            Assert.Equal("hello", dictOut[a2]);

            ser = JsonConvert.SerializeObject(a2);
            var aOut = JsonConvert.DeserializeObject<Address>(ser);

            Assert.True(a2 == aOut);
            Assert.True(a2.Equals(aOut));
        }

        [Fact]
        public void AddressSetFieldsOnObject()
        {
            var a1 = new Address(typeof(PathAddressData), "a&b");
            var pad = new PathAddressData();
            a1.SetAddressFields(pad);
            Assert.Equal("a&b", pad.P);

            var a2 = new Address(typeof(SplitAddressData), "a&001");
            var sad = new SplitAddressData();
            a2.SetAddressFields(sad);
            Assert.Equal("a", sad.A);
            Assert.Equal(1, sad.B);

            var a3 = new Address(typeof(SplitAddressData), "a");
            var sad2 = new SplitAddressData();
            sad2.B = 100;
            a3.SetAddressFields(sad2);
            Assert.Equal("a", sad2.A);
            Assert.Equal(0, sad2.B);

            var a4dict = new Dictionary<string, object>();
            a4dict["_1"] = 2;
            var a4 = new Address(typeof(SplitAddressData), a4dict);

            var sad3 = new SplitAddressData();
            sad3.A = "hello";
            a4.SetAddressFields(sad3);
            Assert.Null(sad3.A);
            Assert.Equal(sad3.B, 2);
        }

        [Fact]
        public void AddressFilter()
        {
            var a1 = new Address(typeof(PathAddressData), "a");
            var paFiltered = a1.GetAsQueryBody<PathAddressData>()(paDatas.AsQueryable()).ToList();
            Assert.Equal(1, paFiltered.Count);
            Assert.Equal("02", paFiltered.First().X);

            var a2 = new Address(typeof(PathAddressData), "");
            paFiltered = a2.GetAsQueryBody<PathAddressData>()(paDatas.AsQueryable()).ToList();
            Assert.Equal(1, paFiltered.Count);
            Assert.Equal("01", paFiltered.First().X);

            var rd = new RouteData();
            rd.Values.Add("_0", "a");
            rd.Values.Add("_1", "b");
            var a3 = new Address(typeof(PathAddressData), rd);
            paFiltered = a3.GetAsQueryBody<PathAddressData>()(paDatas.AsQueryable()).ToList();
            Assert.Equal(1, paFiltered.Count);
            Assert.Equal("03", paFiltered.First().X);

            a1 = new Address(typeof(SplitAddressData), "b");
            var paFiltered2 = a1.GetAsQueryBody<SplitAddressData>()(paDatas2.AsQueryable()).ToList();
            Assert.Equal(0, paFiltered2.Count);

            a2 = new Address(typeof(SplitAddressData), "b&002"); // note 
            paFiltered2 = a2.GetAsQueryBody<SplitAddressData>()(paDatas2.AsQueryable()).ToList();
            Assert.Equal(1, paFiltered2.Count);
            Assert.Equal("02", paFiltered2.First().C);
        }
    }
}
