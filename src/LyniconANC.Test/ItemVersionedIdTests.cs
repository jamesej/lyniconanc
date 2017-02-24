using System;
using System.Collections.Generic;
using System.Linq;
using Lynicon.Attributes;
using LyniconANC.Test.Models;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Models;
using Lynicon.Repositories;
using LyniconANC.Test.Models;
using NUnit.Framework;

namespace LyniconANC.Test
{
    [TestFixture]
    public class ItemVersionedIdTests
    {
        [Test]
        public void ItemVersionedIdEquality()
        {
            Guid id0 = Guid.NewGuid();
            ItemVersion iv0 = new ItemVersion(new Dictionary<string, object> { { "testV", "en-GB" } });
            ItemVersion iv1 = new ItemVersion(new Dictionary<string, object> { { "testV", "es-ES" } });
            var ii0 = new ItemVersionedId(typeof(HeaderContent), id0, iv0);
            var ii1 = new ItemVersionedId(typeof(HeaderContent), new Guid(id0.ToString()), new ItemVersion(iv0));
            var ii2 = new ItemVersionedId(typeof(RestaurantContent), id0, new ItemVersion(iv0));
            var ii3 = new ItemVersionedId(typeof(HeaderContent), Guid.NewGuid(), new ItemVersion(iv0));
            var ii4 = new ItemVersionedId(typeof(HeaderContent), new Guid(id0.ToString()), iv1);

            Assert.IsTrue(ii0.Equals(ii1), ".Equals true");
            Assert.IsTrue(ii0 == ii1, "== true");
            Assert.IsFalse(ii0.Equals(ii2), ".Equals false by type");
            Assert.IsFalse(ii0 == ii2, "== false by type");
            Assert.IsFalse(ii1.Equals(ii3), ".Equals false by id");
            Assert.IsFalse(ii1 == ii3, "== false by id");
            Assert.IsFalse(ii0.Equals(ii4), ".Equals false by version");
            Assert.IsFalse(ii0 == ii4, "== false by version");

            Assert.IsFalse(ii0.GetHashCode() == ii2.GetHashCode(), "hash code by type");
            Assert.IsFalse(ii1.GetHashCode() == ii3.GetHashCode(), "hash code by id");
            Assert.IsFalse(ii0.GetHashCode() == ii4.GetHashCode(), "hash code by version");
        }

        [Test]
        public void ItemVersionedIdConstructors()
        {
            ItemVersion iv0 = new ItemVersion(new Dictionary<string, object> { { "testV", "en-GB" } });

            // ItemId uses ContentType() of the relevant type
            Guid id1 = Guid.NewGuid();
            Type extType = CompositeTypeManager.Instance.ExtendedTypes[typeof(TestData)];
            var ii1 = new ItemVersionedId(extType, id1, iv0);
            Assert.AreEqual(typeof(TestData), ii1.Type);

            // Serialize/Deserialize
            var ii2 = new ItemVersionedId(ii1.ToString());
            Assert.AreEqual(ii2.Type, ii1.Type);
            Assert.AreEqual(ii2.Id, ii1.Id);
            Assert.AreEqual(ii2, ii1);

            // Construct from basic type
            TestData td = Collator.Instance.GetNew<TestData>(new Address(typeof(TestData), "a"));
            td.Id = 5;
            var ii3 = new ItemVersionedId(td);
            Assert.AreEqual(ii3.Id, td.Id);
            Assert.AreEqual(ii3.Version, VersionManager.Instance.CurrentVersion);

            // Construct from container
            Guid id = Guid.NewGuid();
            Guid ident = Guid.NewGuid();
            ContentItem ci = new ContentItem { Id = id, Identity = ident, DataType = typeof(RestaurantContent).FullName };
            var ii4 = new ItemVersionedId(ci);
            Assert.AreEqual(ii4.Id, ident);
            Assert.AreEqual(ii4.Type, typeof(RestaurantContent));
            Assert.AreEqual(ii4.Version, VersionManager.Instance.CurrentVersion);

            // Construct from data item
            RestaurantContent rc = Collator.Instance.GetNew<RestaurantContent>(new Address(typeof(RestaurantContent), "x"));
            var ii5 = new ItemVersionedId(rc);
            Assert.AreEqual(ii5.Id, rc.Identity);
            Assert.AreEqual(ii5.Version, VersionManager.Instance.CurrentVersion);

            // Construct from summary
            RestaurantSummary rs = Collator.Instance.GetSummary<RestaurantSummary>(rc);
            var ii6 = new ItemVersionedId(rs);
            Assert.AreEqual(ii6.Id, rs.Id);
            Assert.AreEqual(ii6.Type, rs.Type);
            Assert.AreEqual(ii6.Version, VersionManager.Instance.CurrentVersion);

            // No empty value allowed
            ContentItem cc = null;
            ItemId iitest = null;
            object otest = null;
            string stest = null;
            Summary summtest = null;
            Assert.Catch(() => new ItemVersionedId(cc));
            Assert.Catch(() => new ItemVersionedId(iitest));
            Assert.Catch(() => new ItemVersionedId(otest));
            Assert.Catch(() => new ItemVersionedId(stest));
            Assert.Catch(() => new ItemVersionedId(summtest));
            Assert.Catch(() => new ItemVersionedId(typeof(RestaurantContent), null, iv0));
            Assert.Catch(() => new ItemVersionedId(null, Guid.NewGuid(), iv0));
            Assert.Catch(() => new ItemVersionedId(typeof(RestaurantContent), Guid.NewGuid(), null));
        }

    }
}
