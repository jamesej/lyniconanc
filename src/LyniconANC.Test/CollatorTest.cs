using System;
using System.Linq;
using System.Collections.Generic;
using Lynicon.Collation;
using Lynicon.Repositories;
using Lynicon.Extensibility;
using Lynicon.Models;
using LyniconANC.Test.Models;
using Xunit;

// Initialise database with test data
//  use ef directly, use appropriate schema for modules in use
// Attach event handlers to run at end of others, handlers store data for checking in class local vars
// 

namespace LyniconANC.Test
{
    [Collection("Lynicon System")]
    public class CollatorTest
    {
        LyniconSystemFixture sys;

        public CollatorTest(LyniconSystemFixture sys)
        {
            this.sys = sys;
        }

        [Fact]
        public void WriteRead()
        {
            var hc = Collator.Instance.GetNew<HeaderContent2>(new Address(typeof(HeaderContent2), "ct-a"));

            hc.Title = "CT Header A";
            hc.Image.Url = "/abc.gif";
            hc.HeaderBody = "xyz";
            hc.SubTests = new List<SubTest> { new SubTest { A = "aab", B = "bbc" } };

            Assert.IsAssignableFrom<ICoreMetadata>(hc);

            Collator.Instance.Set(hc);

            // modify

            hc.HeaderBody = "zzz";
            Collator.Instance.Set(hc);

            var items2 = Collator.Instance.Get<HeaderContent2>(new Address[] { new Address(typeof(HeaderContent2), "ct-a") }).ToList();
            Assert.Equal(1, items2.Count);
            var item = items2[0];
            Assert.Equal(1, item.SubTestsCount);
            Assert.Equal("zzz", item.HeaderBody);

            Assert.IsAssignableFrom<ICoreMetadata>(items2[0]);


            var hc2 = Collator.Instance.GetNew<HeaderContent2>(new Address(typeof(HeaderContent2), "ct-b"));

            hc2.Title = "CT Header B";
            hc2.Image.Url = "/def.gif";
            hc2.HeaderBody = "bbb";

            Assert.IsAssignableFrom<ICoreMetadata>(hc2);

            Collator.Instance.Set(hc2, true);

            var items = Collator.Instance.Get<HeaderContent2>();
            Assert.Equal(2, items.Count(i => (i.Title ?? "").StartsWith("CT")));

            var allHc2 = Collator.Instance.Get<object, object>(new Type[] { typeof(HeaderContent2) }, iq => iq).ToList();
            Assert.True(allHc2.Count > 0 && allHc2.All(o => o is HeaderContent2 && o is ICoreMetadata));

            var itemId = new ItemId(sys.LyniconSystem.Collator, item);
            var item2 = Collator.Instance.Get<HeaderContent2>(itemId);
            Assert.NotNull(item2);
            Assert.Equal(item.Title, item2.Title);
            Assert.IsAssignableFrom<ICoreMetadata>(item2);

            Collator.Instance.Delete(item2);

            var items3 = Collator.Instance.Get<HeaderContent2>();
            Assert.Equal(1, items3.Count(i => (i.Title ?? "").StartsWith("CT")));

            Collator.Instance.Delete(hc2);
        }

        [Fact]
        public void WriteReadBasic()
        {
            var td = Collator.Instance.GetNew<TestData>(new Address(typeof(TestData), "x"));
            td.Value1 = "nnn";
            td.Title = "TitleCT";
            td.Path = "x";
            td.Id = 1;
            Collator.Instance.Set(td, true);

            var item = Collator.Instance.Get<TestData>(new Address(typeof(TestData), "x"));
            Assert.NotNull(item);

            var td2 = Collator.Instance.GetNew<TestData>(new Address(typeof(TestData), "y"));
            td2.Value1 = "nnn";
            td2.Title = "TitleCT2";
            td2.Path = "y";
            td2.Id = 2;
            Collator.Instance.Set(td2, true);

            var items = Collator.Instance.Get<TestData, TestData>(iq => iq.Where(d =>(d.Title ?? "").StartsWith("TitleCT")));
            Assert.Equal(2, items.Count());

            var itemId = new ItemId(sys.LyniconSystem.Collator, item);
            var item2 = Collator.Instance.Get<TestData>(itemId);
            Assert.NotNull(item2);
            Assert.Equal("TitleCT", item2.Title);
            Assert.Equal(item2.Id, item.Id);

            var item3 = Collator.Instance.Get<TestData>(new Address(typeof(TestData), "x"));
            Assert.NotNull(item3);
            Assert.Equal("TitleCT", item3.Title);
            Assert.Equal(item3.Id, item.Id);

            var summ = Collator.Instance.Get<TestDataSummary>(itemId);
            Assert.NotNull(summ);
            Assert.Equal("TitleCT", summ.Title);
            Assert.Equal("/testd/x", summ.Url);

            var summ2 = Collator.Instance.Get<TestDataSummary>(new Address(typeof(TestData), "y"));
            Assert.NotNull(summ2);
            Assert.Equal("TitleCT2", summ2.Title);
            Assert.Equal("/testd/y", summ2.Url);

            Collator.Instance.Delete(item2);

            var items2 = Collator.Instance.Get<TestData, TestData>(iq => iq.Where(d => (d.Title ?? "").StartsWith("TitleCT")));
            Assert.Equal(1, items2.Count()); // There is an extra item which holds shared data
        }

        [Fact]
        public void Polymorphic()
        {
            var s1 = Collator.Instance.GetNew<Sub1TContent>(new Address(typeof(Sub1TContent), "s1"));
            s1.Title = "Sub1";
            s1.SomeStuff = new MinHtml("<b>this</b>");
            var s2 = Collator.Instance.GetNew<Sub2TContent>(new Address(typeof(Sub2TContent), "s2"));
            s2.Title = "Sub2";
            s2.Links.Add(new Link { Content = "linky", Url = "/abc" });
        }

        [Fact]
        public void Summaries()
        {
            var hc = Collator.Instance.GetNew<HeaderContent>(new Address(typeof(HeaderContent), "ct-c"));

            hc.Title = "CT Header A";
            hc.Image.Url = "/abc.gif";
            hc.HeaderBody = "xyz";
            hc.SubTests = new List<SubTest> { new SubTest { A = "aab", B = "bbc" } };

            Collator.Instance.Set(hc);

            var item = Collator.Instance.Get<HeaderSummary>(new Address(typeof(HeaderContent), "ct-c"));
            Assert.NotNull(item);
            Assert.Equal(1, item.SubTestsCount);
            Assert.Equal("CT Header A", item.Title);
            Assert.Equal(typeof(HeaderContent), item.Type);
            Assert.Equal("/header/ct-c", item.Url);
            Assert.NotNull(item.Version);

            var hc2 = Collator.Instance.GetNew<HeaderContent>(new Address(typeof(HeaderContent), "ct-d"));

            hc2.Title = "CT Header B";
            hc2.Image.Url = "/def.gif";
            hc2.HeaderBody = "bbb";
            hc2.SubTests = new List<SubTest> { new SubTest { A = "aab", B = "bbc" }, new SubTest { A = "aabx", B = "bbcx" } };

            Collator.Instance.Set(hc2, true);

            var items = Collator.Instance.Get<HeaderSummary>();
            Assert.Equal(2, items.Count(hs => (hs.Title ?? "").StartsWith("CT")));

            var itemId = new ItemId(item);
            var item2 = Collator.Instance.Get<HeaderContent>(itemId);
            Assert.NotNull(item2);
            Assert.Equal(item.Title, item2.Title);

            Collator.Instance.Delete(item2);

            var items2 = Collator.Instance.Get<HeaderSummary>();
            Assert.Equal(1, items2.Count(hs => (hs.Title ?? "").StartsWith("CT")));
        }

        [Fact]
        public void SummaryFields()
        {
            Type tExt = sys.LyniconSystem.Extender[typeof(TestData)];
            var summaryFieldNames = Collator.Instance.ContainerSummaryFields(tExt)
                .Select(pi => pi.Name)
                .OrderBy(nm => nm)
                .ToList();

            Assert.True(summaryFieldNames.SequenceEqual(new List<string> { "ExtData", "Id", "Path", "Title", "Value1" }));
        }
    }
}
