using System;
using System.Collections.Generic;
using System.Linq;
using Lynicon.Attributes;
using LyniconANC.Autotests.Models;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Models;
using Lynicon.Repositories;
using LyniconANC.Test.Models;
using NUnit.Framework;

// Initialise database with test data
//  use ef directly, use appropriate schema for modules in use
// Attach event handlers to run at end of others, handlers store data for checking in class local vars
// 

namespace LyniconANC.Autotests
{
    [TestFixture]
    public class ItemIdTests
    {
        [Test]
        public void ItemIdEquality()
        {
            Guid id0 = Guid.NewGuid();
            var ii0 = new ItemId(typeof(HeaderContent), id0);
            var ii1 = new ItemId(typeof(HeaderContent), new Guid(id0.ToString()));
            var ii2 = new ItemId(typeof(RestaurantContent), id0);
            var ii3 = new ItemId(typeof(HeaderContent), Guid.NewGuid());

            Assert.IsTrue(ii0.Equals(ii1), ".Equals true");
            Assert.IsTrue(ii0 == ii1, "== true");
            Assert.IsFalse(ii0.Equals(ii2), ".Equals false by type");
            Assert.IsFalse(ii0 == ii2, "== false by type");
            Assert.IsFalse(ii1.Equals(ii3), ".Equals false by id");
            Assert.IsFalse(ii1 == ii3, "== false by id");

            Assert.IsFalse(ii0.GetHashCode() == ii2.GetHashCode(), "hash code by type");
            Assert.IsFalse(ii1.GetHashCode() == ii3.GetHashCode(), "hash code by id");
        }

        [Test]
        public void ItemIdConstructors()
        {
            // ItemId uses ContentType() of the relevant type
            Guid id1 = Guid.NewGuid();
            Type extType = CompositeTypeManager.Instance.ExtendedTypes[typeof(TestData)];
            var ii1 = new ItemId(extType, id1);
            Assert.AreEqual(typeof(TestData), ii1.Type);

            // Serialize/Deserialize
            var ii2 = new ItemId(ii1.ToString());
            Assert.AreEqual(ii2.Type, ii1.Type);
            Assert.AreEqual(ii2.Id, ii1.Id);
            Assert.AreEqual(ii2, ii1);

            // Construct from basic type
            TestData td = Collator.Instance.GetNew<TestData>(new Address(typeof(TestData), "a"));
            td.Id = 5;
            var ii3 = new ItemId(td);
            Assert.AreEqual(ii3.Id, td.Id);

            // Construct from container
            Guid id = Guid.NewGuid();
            Guid ident = Guid.NewGuid();
            ContentItem ci = new ContentItem { Id = id, Identity = ident, DataType = typeof(RestaurantContent).FullName };
            var ii4 = new ItemId(ci);
            Assert.AreEqual(ii4.Id, ident);
            Assert.AreEqual(ii4.Type, typeof(RestaurantContent));

            // Construct from data item
            RestaurantContent rc = Collator.Instance.GetNew<RestaurantContent>(new Address(typeof(RestaurantContent), "x"));
            var ii5 = new ItemId(rc);
            Assert.AreEqual(ii5.Id, rc.Identity);

            // Construct from summary
            RestaurantSummary rs = Collator.Instance.GetSummary<RestaurantSummary>(rc);
            var ii6 = new ItemId(rs);
            Assert.AreEqual(ii6.Id, rs.Id);
            Assert.AreEqual(ii6.Type, rs.Type);
        }

    }
}
