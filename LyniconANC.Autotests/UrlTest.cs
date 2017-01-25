using System;
using System.Linq;
using System.Collections.Generic;
using Lynicon.Collation;
using Lynicon.Repositories;
using Lynicon.Extensibility;
using Lynicon.Relations;
using NUnit.Framework;
using LyniconANC.Test.Models;

// Initialise database with test data
//  use ef directly, use appropriate schema for modules in use
// Attach event handlers to run at end of others, handlers store data for checking in class local vars
// 

namespace LyniconANC.Autotests
{
    [TestFixture]
    public class UrlTest
    {
        [Test]
        public void UrlMoveClash()
        {
            var hc = Collator.Instance.GetNew<HeaderContent>(new Address(typeof(HeaderContent), "ut-a"));

            hc.Title = "UT Header A";
            hc.HeaderBody = "zczczc";

            Collator.Instance.Set(hc);

            var hc2 = Collator.Instance.GetNew<HeaderContent>(new Address(typeof(HeaderContent), "ut-b"));

            hc2.Title = "UT Header B";
            hc2.HeaderBody = "qwqwqw";

            Collator.Instance.Set(hc2);

            ItemId hc2iid = new ItemId(hc2);
            Collator.Instance.MoveAddress(hc2iid, new Address(typeof(HeaderContent), "ut-c"));

            Exception ex = null;
            try
            {
                Collator.Instance.MoveAddress(hc2iid, new Address(typeof(HeaderContent), "ut-a"));
            }
            catch (ApplicationException appEx)
            {
                ex = appEx;
            }
            Assert.IsNotNull(ex, "Failed to block move to an existing data route url");

            // Does not now consider standard urls
            //ex = null;
            //try
            //{
            //    Collator.Instance.MoveAddress(hc2iid, new Address(typeof(HeaderContent), "ut-x"));
            //}
            //catch (ApplicationException appEx)
            //{
            //    ex = appEx;
            //}
            //Assert.IsNotNull(ex, "Failed to block move to an existing standard url");
        }
    }
}
