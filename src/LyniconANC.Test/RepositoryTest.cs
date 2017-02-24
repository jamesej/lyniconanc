using System;
using System.Linq;
using System.Collections.Generic;
using Lynicon.Collation;
using Lynicon.Repositories;
using Lynicon.Extensibility;
using NUnit.Framework;
using LyniconANC.Test.Models;

// Initialise database with test data
//  use ef directly, use appropriate schema for modules in use
// Attach event handlers to run at end of others, handlers store data for checking in class local vars
// 

namespace LyniconANC.Test
{
    [TestFixture]
    public class RepositoryTest
    {
        [Test]
        public void WriteRead()
        {
            var ci = Repository.Instance.New<ContentItem>();
            Assert.AreEqual(ci.Id, Guid.Empty, "Content item id not initialised to empty guid");

            var hc = new HeaderContent();
            hc.Title = "Header A";
            hc.Image.Url = "/abc.gif";
            hc.HeaderBody = "xyz";
            ci.SetContent(hc);
            ci.Path = "rt-a";
            Assert.AreEqual(ci.Title, "Header A", "Title not built on SetContent");
            Assert.AreEqual(((HeaderSummary)ci.GetSummary()).Image.Url, "/abc.gif", "Summary not built on SetContent");

            Repository.Instance.Set(ci);

            var cont = Repository.Instance.GetByPath(typeof(HeaderContent), new List<string> { "rt-a" }).FirstOrDefault();
            Assert.IsNotNull(cont, "Get by path");

            var itemId = new ItemId(cont);
            var cont2 = Repository.Instance.Get<ContentItem>(new ItemId[] { itemId }).FirstOrDefault();
            Assert.IsNotNull(cont2, "Get by Id");
            Assert.AreEqual(cont2.Id, cont.Id, "Get right item by Id");
        }

        [Test]
        public void WriteReadBasic()
        {
            var td = Repository.Instance.New<TestData>();
            td.Value1 = "nnn";
            td.Path = "rt-x";
            td.Id = 10;
            Repository.Instance.Set(td, true);

            var cont = Repository.Instance.Get<TestData>(typeof(TestData),
                iq => iq.Where(x => x.Path == "rt-x")).FirstOrDefault();
            Assert.IsNotNull(cont, "GetByPath");

            var itemId = new ItemId(cont);
            var cont2 = Repository.Instance.Get<TestData>(new ItemId[] { itemId }).FirstOrDefault();
            Assert.IsNotNull(cont2, "Get by Id");
            Assert.AreEqual(cont2.Id, cont.Id, "Get right item by Id");

            var cont3 = Repository.Instance.Get<TestData>(typeof(TestData), new Address(typeof(TestData), "rt-x")).FirstOrDefault();
            Assert.IsNotNull(cont3, "Get by Address");
            Assert.AreEqual(cont3.Id, cont.Id, "Get right item by Address");
        }
    }
}
