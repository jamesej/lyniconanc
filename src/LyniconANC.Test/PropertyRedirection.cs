using System;
using System.Collections.Generic;
using System.Linq;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Models;
using Lynicon.Repositories;
using Lynicon.Utility;
using NUnit.Framework;
using LyniconANC.Test.Models;

// Initialise database with test data
//  use ef directly, use appropriate schema for modules in use
// Attach event handlers to run at end of others, handlers store data for checking in class local vars
// 

namespace LyniconANC.Test
{
    [TestFixture]
    public class PropertyRedirection
    {
        PropertyRedirectContent prc0, prc1, prc2, prc3;
        List<PropertyRedirectContent> prcs;

        [Test]
        public void CommonPropertyRedirect()
        {
            var aaAddr = new Address(typeof(PropertyRedirectContent), "aa");
            prc0 = Collator.Instance.GetNew<PropertyRedirectContent>(aaAddr);
            prc0.Title = "Item 0";
            prc0.Common = "Common Text";
            prc0.ExternalVal = "External";
            Collator.Instance.Set(aaAddr, prc0);

            // GetNew should set common values
            var bbAddr = new Address(typeof(PropertyRedirectContent), "bb");
            prc1 = Collator.Instance.GetNew<PropertyRedirectContent>(bbAddr);

            Assert.IsTrue(prc1.Common == "Common Text", "new record has common (content record)");

            prc1.Title = "Item 1";
            Collator.Instance.Set(bbAddr, prc1);

            Assert.IsTrue(prc1.Common == "Common Text", "saved record has common (content record)");

            prc1.Common = "Changed";
            Collator.Instance.Set(prc1);
            prc0 = Collator.Instance.Get<PropertyRedirectContent>(aaAddr);

            Assert.IsTrue(prc0.Common == "Changed", "update common affects all");

            prcs = Collator.Instance.Get<PropertyRedirectContent, ContentItem>(
                iq => iq.Where(ci => ci.Title == "Item 0")
                ).ToList();

            Assert.IsTrue(prcs.Count == 1, "only one prc titled 'Item 0'");
            Assert.IsTrue(prcs.First().Common == "Changed", "common on get by query");

            prc2 = Collator.Instance.Get<PropertyRedirectContent>(prc0.ItemId);

            Assert.IsTrue(prc2.Common == "Changed", "common on get by id");


        }
    }
}
