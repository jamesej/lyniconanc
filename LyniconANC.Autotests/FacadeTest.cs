using System;
using System.Collections.Generic;
using System.Linq;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Linq;
using Lynicon.Models;
using Lynicon.Repositories;
using NUnit.Framework;

// Initialise database with test data
//  use ef directly, use appropriate schema for modules in use
// Attach event handlers to run at end of others, handlers store data for checking in class local vars
// 

namespace LyniconANC.Autotests
{
    [TestFixture]
    public class FacadeTests
    {
        private class C0
        {
            public int X { get; set; }
        }

        private class C1 : C0, IHasZ
        {
            public string Y { get; set; }

            public string Z { get; set; }
        }

        private interface IHasZ
        {
            string Z { get; set; }
        }

        private class CX0 : IHasZ
        {
            public string Z { get; set; }
        }

        [Test]
        public void TestAsFacade()
        {
            var l1 = new List<C0>();
            l1.Add(new C0 { X = 1 });
            l1.Add(new C1 { X = 1, Y = "hello", Z = "goodbye" });
            l1.Add(new C0 { X = 2 });
            l1.Add(new C0 { X = 0 });

            var l2 = new List<C1>();
            l2.Add(new C1 { X = 0, Y = "xyz", Z = "zzz" });
            l2.Add(new C1 { X = 2, Y = "mmm", Z = "nnn" });
            l2.Add(new C1 { X = 1, Y = null, Z = "qqq" });

            var l3 = new List<IHasZ>();
            l3.Add(new C1 { X = 2, Y = "000", Z = "abc" });
            l3.Add(new CX0 { Z = "yyy" });
            l3.Add(new CX0 { Z = "abc" });

            var l4 = new List<IHasZ>();

            IQueryable l1Q = l1.AsQueryable();
            IQueryable l2Q = l2.AsQueryable();
            var out0 = l1Q.AsFacade<C0>().Where(x => x.X == 1).ToList();
            Assert.AreEqual(out0.Count, 2);
            var out1 = l2Q.AsFacade<C0>().Where(x => x.X == 1).ToList();
            Assert.AreEqual(out1.Count, 1);

            IQueryable l3Q = l3.AsQueryable();
            var out3 = l3Q.AsFacade<IHasZ>().Where(x => x.Z == "abc").ToList();
            Assert.AreEqual(out3.Count, 2);
        }

    }
}
