using System;
using System.Linq;
using System.Collections.Generic;
using Lynicon.Collation;
using Lynicon.Repositories;
using Lynicon.Extensibility;
using LyniconANC.Test.Models;
using Xunit;

// Initialise database with test data
//  use ef directly, use appropriate schema for modules in use
// Attach event handlers to run at end of others, handlers store data for checking in class local vars
// 

namespace LyniconANC.Test
{
    [Collection("Lynicon System")]
    public class RepositoryTest
    {
        LyniconSystemFixture sys;

        public RepositoryTest(LyniconSystemFixture sys)
        {
            this.sys = sys;
        }

        [Fact]
        public void WriteRead()
        {
            var ci = Repository.Instance.New<ContentItem>();
            Assert.Equal(ci.Id, Guid.Empty);

            var hc = new HeaderContent();
            hc.Title = "Header A";
            hc.Image.Url = "/abc.gif";
            hc.HeaderBody = "xyz";
            ci.SetContent(hc);
            ci.Path = "rt-a";
            Assert.Equal(ci.Title, "Header A");
            Assert.Equal(((HeaderSummary)ci.GetSummary()).Image.Url, "/abc.gif");

            Repository.Instance.Set(ci);

            var cont = Repository.Instance.GetByPath(typeof(HeaderContent), new List<string> { "rt-a" }).FirstOrDefault();
            Assert.NotNull(cont);

            var itemId = new ItemId(cont);
            var cont2 = Repository.Instance.Get<ContentItem>(new ItemId[] { itemId }).FirstOrDefault();
            Assert.NotNull(cont2);
            Assert.Equal(cont2.Id, cont.Id);
        }

        [Fact]
        public void WriteReadBasic()
        {
            var td = Repository.Instance.New<TestData>();
            td.Value1 = "nnn";
            td.Path = "rt-x";
            td.Id = 10;
            Repository.Instance.Set(td, true);

            var cont = Repository.Instance.Get<TestData>(typeof(TestData),
                iq => iq.Where(x => x.Path == "rt-x")).FirstOrDefault();
            Assert.NotNull(cont);

            var itemId = new ItemId(cont);
            var cont2 = Repository.Instance.Get<TestData>(new ItemId[] { itemId }).FirstOrDefault();
            Assert.NotNull(cont2);
            Assert.Equal(cont2.Id, cont.Id);

            var cont3 = Repository.Instance.Get<TestData>(typeof(TestData), new Address(typeof(TestData), "rt-x")).FirstOrDefault();
            Assert.NotNull(cont3);
            Assert.Equal(cont3.Id, cont.Id);
        }
    }
}
