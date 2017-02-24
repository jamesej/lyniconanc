using System;
using System.Linq;
using System.Collections.Generic;
using Lynicon.Collation;
using Lynicon.Repositories;
using Lynicon.Extensibility;
using NUnit.Framework;
using Lynicon.Models;
using LyniconANC.Test.Models;

// Initialise database with test data
//  use ef directly, use appropriate schema for modules in use
// Attach event handlers to run at end of others, handlers store data for checking in class local vars
// 

namespace LyniconANC.Test
{
    [TestFixture]
    public class CollatorTest
    {
        [Test]
        public void WriteRead()
        {
            var hc = Collator.Instance.GetNew<HeaderContent2>(new Address(typeof(HeaderContent2), "ct-a"));

            hc.Title = "CT Header A";
            hc.Image.Url = "/abc.gif";
            hc.HeaderBody = "xyz";
            hc.SubTests = new List<SubTest> { new SubTest { A = "aab", B = "bbc" } };

            Collator.Instance.Set(hc);

            var item = Collator.Instance.Get<HeaderContent2>(new Address(typeof(HeaderContent2), "ct-a"));
            Assert.IsNotNull(item, "Get by path");
            Assert.AreEqual(item.SubTestsCount, 1);

            var hc2 = Collator.Instance.GetNew<HeaderContent2>(new Address(typeof(HeaderContent2), "ct-b"));

            hc2.Title = "CT Header B";
            hc2.Image.Url = "/def.gif";
            hc2.HeaderBody = "bbb";

            Collator.Instance.Set(hc2, true);

            var items = Collator.Instance.Get<HeaderContent2>();
            Assert.AreEqual(2, items.Count(i => (i.Title ?? "").StartsWith("CT")), "Get all items");

            var itemId = new ItemId(item);
            var item2 = Collator.Instance.Get<HeaderContent2>(itemId);
            Assert.IsNotNull(item2, "Get by Id");
            Assert.AreEqual(item.Title, item2.Title, "Get right item by Id");

            Collator.Instance.Delete(item2);

            var items2 = Collator.Instance.Get<HeaderContent2>();
            Assert.AreEqual(1, items2.Count(i => (i.Title ?? "").StartsWith("CT")), "Delete");

            Collator.Instance.Delete(hc2);
        }

        [Test]
        public void WriteReadBasic()
        {
            var td = Collator.Instance.GetNew<TestData>(new Address(typeof(TestData), "x"));
            td.Value1 = "nnn";
            td.Title = "TitleCT";
            td.Path = "x";
            td.Id = 1;
            Collator.Instance.Set(td, true);

            var item = Collator.Instance.Get<TestData>(new Address(typeof(TestData), "x"));
            Assert.IsNotNull(item, "GetByPath");

            var td2 = Collator.Instance.GetNew<TestData>(new Address(typeof(TestData), "y"));
            td2.Value1 = "nnn";
            td2.Title = "TitleCT2";
            td2.Path = "y";
            td2.Id = 2;
            Collator.Instance.Set(td2, true);

            var items = Collator.Instance.Get<TestData, TestData>(iq => iq.Where(d =>(d.Title ?? "").StartsWith("TitleCT")));
            Assert.AreEqual(2, items.Count(), "Get all items");

            var itemId = new ItemId(item);
            var item2 = Collator.Instance.Get<TestData>(itemId);
            Assert.IsNotNull(item2, "Get by Id");
            Assert.AreEqual("TitleCT", item2.Title, "Item has correct Title (got by Id)");
            Assert.AreEqual(item2.Id, item.Id, "Get right item by Id");

            var item3 = Collator.Instance.Get<TestData>(new Address(typeof(TestData), "x"));
            Assert.IsNotNull(item3, "Get by Address");
            Assert.AreEqual("TitleCT", item3.Title, "Item has correct Title (got by Address)");
            Assert.AreEqual(item3.Id, item.Id, "Get right item by Address");

            var summ = Collator.Instance.Get<TestDataSummary>(itemId);
            Assert.IsNotNull(summ, "Get summary by id");
            Assert.AreEqual("TitleCT", summ.Title, "summary by id has correct Title");
            Assert.AreEqual("/testd/x", summ.Url, "summary by id has correct url");

            var summ2 = Collator.Instance.Get<TestDataSummary>(new Address(typeof(TestData), "y"));
            Assert.IsNotNull(summ2, "Get summary by path");
            Assert.AreEqual("TitleCT2", summ2.Title, "summary by path has correct Title");
            Assert.AreEqual("/testd/y", summ2.Url, "summary by path has correct url");

            Collator.Instance.Delete(item2);

            var items2 = Collator.Instance.Get<TestData, TestData>(iq => iq.Where(d => (d.Title ?? "").StartsWith("TitleCT")));
            Assert.AreEqual(1, items2.Count(), "Delete"); // There is an extra item which holds shared data
        }

        [Test]
        public void Polymorphic()
        {
            var s1 = Collator.Instance.GetNew<Sub1TContent>(new Address(typeof(Sub1TContent), "s1"));
            s1.Title = "Sub1";
            s1.SomeStuff = new MinHtml("<b>this</b>");
            var s2 = Collator.Instance.GetNew<Sub2TContent>(new Address(typeof(Sub2TContent), "s2"));
            s2.Title = "Sub2";
            s2.Links.Add(new Link { Content = "linky", Url = "/abc" });
        }

        [Test]
        public void Summaries()
        {
            var hc = Collator.Instance.GetNew<HeaderContent>(new Address(typeof(HeaderContent), "ct-c"));

            hc.Title = "CT Header A";
            hc.Image.Url = "/abc.gif";
            hc.HeaderBody = "xyz";
            hc.SubTests = new List<SubTest> { new SubTest { A = "aab", B = "bbc" } };

            Collator.Instance.Set(hc);

            var item = Collator.Instance.Get<HeaderSummary>(new Address(typeof(HeaderContent), "ct-c"));
            Assert.IsNotNull(item, "Get summary by path");
            Assert.AreEqual(1, item.SubTestsCount, "Summary computed property");
            Assert.AreEqual("CT Header A", item.Title);
            Assert.AreEqual(typeof(HeaderContent), item.Type);
            Assert.AreEqual("/header/ct-c", item.Url);
            Assert.IsNotNull(item.Version, "Version has a value");

            var hc2 = Collator.Instance.GetNew<HeaderContent>(new Address(typeof(HeaderContent), "ct-d"));

            hc2.Title = "CT Header B";
            hc2.Image.Url = "/def.gif";
            hc2.HeaderBody = "bbb";
            hc2.SubTests = new List<SubTest> { new SubTest { A = "aab", B = "bbc" }, new SubTest { A = "aabx", B = "bbcx" } };

            Collator.Instance.Set(hc2, true);

            var items = Collator.Instance.Get<HeaderSummary>();
            Assert.AreEqual(2, items.Count(hs => (hs.Title ?? "").StartsWith("CT")), "Get all summaries");

            var itemId = new ItemId(item);
            var item2 = Collator.Instance.Get<HeaderContent>(itemId);
            Assert.IsNotNull(item2, "Get summary by Id");
            Assert.AreEqual(item.Title, item2.Title, "Get right summary by Id");

            Collator.Instance.Delete(item2);

            var items2 = Collator.Instance.Get<HeaderSummary>();
            Assert.AreEqual(1, items2.Count(hs => (hs.Title ?? "").StartsWith("CT")), "Delete");
        }
    }
}
