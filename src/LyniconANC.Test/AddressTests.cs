using System;
using System.Collections.Generic;
using System.Linq;
using Lynicon.Attributes;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Models;
using Lynicon.Repositories;
using NUnit.Framework;
using LyniconANC.Test.Models;
using LyniconANC.Test.Models;
using Microsoft.AspNetCore.Routing;

// Initialise database with test data
//  use ef directly, use appropriate schema for modules in use
// Attach event handlers to run at end of others, handlers store data for checking in class local vars
// 

namespace LyniconANC.Test
{
    [TestFixture]
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

        [Test]
        public void AddressEquality()
        {
            Dictionary<string, object> address = new Dictionary<string, object> { { "Existence", "Exists" }, { "Published", false } };
            var ii0 = new Address(typeof(HeaderContent), "abc&def");
            var ii1 = new Address(typeof(HeaderContent), "abc&def");
            var ii2 = new Address(typeof(RestaurantContent), "abc&def");
            var ii3 = new Address(typeof(HeaderContent), "abc");

            Assert.IsTrue(ii0.Equals(ii1), ".Equals true");
            Assert.IsTrue(ii0 == ii1, "== true");
            Assert.IsFalse(ii0.Equals(ii2), ".Equals false by type");
            Assert.IsFalse(ii0 == ii2, "== false by different val");
            Assert.IsFalse(ii1.Equals(ii3), ".Equals false by missing key");
            Assert.IsFalse(ii1 == ii3, "== false by missing key");

            Assert.IsFalse(ii0.GetHashCode() == ii2.GetHashCode(), "hash code by type");
            Assert.IsFalse(ii1.GetHashCode() == ii3.GetHashCode(), "hash code by missing key");
        }

        [Test]
        public void AddressPathConstructor()
        {
            string path = null;
            var a1 = new Address(typeof(HeaderContent), path);
            Assert.AreEqual(0, a1.Count);

            var a2 = new Address(typeof(HeaderContent), "abc");
            Assert.AreEqual("abc", a2["_0"]);
            Assert.AreEqual(1, a2.Count);

            var a3 = new Address(typeof(HeaderContent), "p&qr");
            Assert.AreEqual("p", a3["_0"]);
            Assert.AreEqual("qr", a3["_1"]);
            Assert.AreEqual(2, a3.Count);
        }

        [Test]
        public void AddressRouteDataConstructor()
        {
            var rd = new RouteData();
            var a1 = new Address(typeof(HeaderContent), rd);
            Assert.AreEqual("", a1.GetAsContentPath());

            rd.Values.Add("_0", "qqq");
            var a2 = new Address(typeof(HeaderContent), rd);
            Assert.AreEqual("qqq", a2.GetAsContentPath());

            rd.Values.Add("_1", "abc");
            var a3 = new Address(typeof(HeaderContent), rd);
            Assert.AreEqual("qqq&abc", a3.GetAsContentPath());
            Assert.AreNotEqual(a2.GetAsContentPath(), a3.GetAsContentPath(), "Address immutable via changing constructor arg");
        }

        [Test]
        public void AddressObjectConstructor()
        {
            var a1 = new Address(paDatas2[0]);
            Assert.AreEqual("a", a1["_0"]);
            Assert.AreEqual(1, a1["_1"]);
            Assert.AreEqual("a&001", a1.GetAsContentPath());
            Assert.AreEqual("001", a1.GetAsString("_1"));

            var a2 = new Address(paDatas[2]);
            Assert.AreEqual("a", a2["_0"]);
            Assert.AreEqual("b", a2["_1"]);
            Assert.AreEqual("a&b", a2.GetAsContentPath());
        }

        [Test]
        public void AddressSerialization()
        {
            var a1 = new Address("LyniconANC.Test.Models.HeaderContent:_0=a&_1=1");
            Assert.AreEqual("a", a1["_0"]);
            Assert.AreEqual("1", a1["_1"]);
            Assert.AreEqual("HeaderContent", a1.Type.Name);

            var a2 = new Address();
            a2.Type = typeof(HeaderContent);
            a2["_0"] = "a";
            a2["_1"] = 1;
            Assert.AreEqual("LyniconANC.Test.Models.HeaderContent:_0=a&_1=1", a2.ToString());
        }

        [Test]
        public void AddressSetFieldsOnObject()
        {
            var a1 = new Address(typeof(PathAddressData), "a&b");
            var pad = new PathAddressData();
            a1.SetAddressFields(pad);
            Assert.AreEqual("a&b", pad.P);

            var a2 = new Address(typeof(SplitAddressData), "a&001");
            var sad = new SplitAddressData();
            a2.SetAddressFields(sad);
            Assert.AreEqual("a", sad.A);
            Assert.AreEqual(1, sad.B);

            var a3 = new Address(typeof(SplitAddressData), "a");
            var sad2 = new SplitAddressData();
            sad2.B = 100;
            a3.SetAddressFields(sad2);
            Assert.AreEqual("a", sad2.A);
            Assert.AreEqual(0, sad2.B);

            var a4 = new Address();
            a4.Type = typeof(SplitAddressData);
            a4["_1"] = 2;
            var sad3 = new SplitAddressData();
            sad3.A = "hello";
            a4.SetAddressFields(sad3);
            Assert.IsNull(sad3.A);
            Assert.AreEqual(sad3.B, 2);
        }

        [Test]
        public void AddressFilter()
        {
            var a1 = new Address(typeof(PathAddressData), "a");
            var paFiltered = a1.GetAsQueryBody<PathAddressData>()(paDatas.AsQueryable()).ToList();
            Assert.AreEqual(1, paFiltered.Count);
            Assert.AreEqual("02", paFiltered.First().X);

            var a2 = new Address(typeof(PathAddressData), "");
            paFiltered = a2.GetAsQueryBody<PathAddressData>()(paDatas.AsQueryable()).ToList();
            Assert.AreEqual(1, paFiltered.Count);
            Assert.AreEqual("01", paFiltered.First().X);

            var rd = new RouteData();
            rd.Values.Add("_0", "a");
            rd.Values.Add("_1", "b");
            var a3 = new Address(typeof(PathAddressData), rd);
            paFiltered = a3.GetAsQueryBody<PathAddressData>()(paDatas.AsQueryable()).ToList();
            Assert.AreEqual(1, paFiltered.Count);
            Assert.AreEqual("03", paFiltered.First().X);

            a1 = new Address(typeof(SplitAddressData), "b");
            var paFiltered2 = a1.GetAsQueryBody<SplitAddressData>()(paDatas2.AsQueryable()).ToList();
            Assert.AreEqual(0, paFiltered2.Count);

            a2 = new Address(typeof(SplitAddressData), "b&002"); // note 
            paFiltered2 = a2.GetAsQueryBody<SplitAddressData>()(paDatas2.AsQueryable()).ToList();
            Assert.AreEqual(1, paFiltered2.Count);
            Assert.AreEqual("02", paFiltered2.First().C);
        }
    }
}
