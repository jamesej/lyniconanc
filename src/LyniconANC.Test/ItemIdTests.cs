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
using Newtonsoft.Json;
using Xunit;

// Initialise database with test data
//  use ef directly, use appropriate schema for modules in use
// Attach event handlers to run at end of others, handlers store data for checking in class local vars
// 

namespace LyniconANC.Test
{
    [Collection("Lynicon System")]
    public class ItemIdTests
    {
        LyniconSystemFixture sys;

        public ItemIdTests(LyniconSystemFixture sys)
        {
            this.sys = sys;
        }

        [Fact]
        public void ItemIdEquality()
        {
            Guid id0 = Guid.NewGuid();
            var ii0 = new ItemId(typeof(HeaderContent), id0);
            var ii1 = new ItemId(typeof(HeaderContent), new Guid(id0.ToString()));
            var ii2 = new ItemId(typeof(RestaurantContent), id0);
            var ii3 = new ItemId(typeof(HeaderContent), Guid.NewGuid());

            Assert.True(ii0.Equals(ii1), ".Equals true");
            Assert.True(ii0 == ii1, "== true");
            Assert.False(ii0.Equals(ii2));
            Assert.False(ii0 == ii2);
            Assert.False(ii1.Equals(ii3));
            Assert.False(ii1 == ii3);

            Assert.False(ii0.GetHashCode() == ii2.GetHashCode(), "hash code by type");
            Assert.False(ii1.GetHashCode() == ii3.GetHashCode(), "hash code by id");
        }

        [Fact]
        public void ItemIdConstructors()
        {
            // ItemId uses ContentType() of the relevant type
            Guid id1 = Guid.NewGuid();
            Type extType = sys.LyniconSystem.Extender[typeof(TestData)];
            var ii1 = new ItemId(extType, id1);
            Assert.Equal(typeof(TestData), ii1.Type);

            // Serialize/Deserialize
            var ii2 = new ItemId(ii1.ToString());
            Assert.Equal(ii2.Type, ii1.Type);
            Assert.Equal(ii2.Id, ii1.Id);
            Assert.Equal(ii2, ii1);

            // Construct from basic type
            TestData td = Collator.Instance.GetNew<TestData>(new Address(typeof(TestData), "a"));
            td.Id = 5;
            var ii3 = new ItemId(td);
            Assert.Equal(ii3.Id, td.Id);

            // Construct from container
            Guid id = Guid.NewGuid();
            Guid ident = Guid.NewGuid();
            ContentItem ci = new ContentItem { Id = id, Identity = ident, DataType = typeof(RestaurantContent).FullName };
            var ii4 = new ItemId(ci);
            Assert.Equal(ii4.Id, ident);
            Assert.Equal(ii4.Type, typeof(RestaurantContent));

            // Construct from data item
            RestaurantContent rc = Collator.Instance.GetNew<RestaurantContent>(new Address(typeof(RestaurantContent), "x"));
            var ii5 = new ItemId(rc);
            Assert.Equal(ii5.Id, ((ICoreMetadata)rc).Identity);

            // Construct from summary
            RestaurantSummary rs = Collator.Instance.GetSummary<RestaurantSummary>(rc);
            var ii6 = new ItemId(rs);
            Assert.Equal(ii6.Id, rs.Id);
            Assert.Equal(ii6.Type, rs.Type);

            // No empty value allowed
            ContentItem cc = null;
            ItemId iitest = null;
            object otest = null;
            string stest = null;
            Summary summtest = null;
            Assert.Throws<NullReferenceException>(() => new ItemId(cc));
            Assert.Throws<NullReferenceException>(() => new ItemId(iitest));
            Assert.Throws<NullReferenceException>(() => new ItemId(otest));
            Assert.Throws<ArgumentException>(() =>
            {
                var x = new ItemId(stest);
            });
            Assert.Throws<NullReferenceException>(() => new ItemId(summtest));
            Assert.Throws<ArgumentException>(() => new ItemId(typeof(RestaurantContent), null));
            Assert.Throws<ArgumentException>(() => new ItemId((Type)null, Guid.NewGuid()));
        }

        [Fact]
        public void ItemIdSerialization()
        {
            Guid id0 = Guid.NewGuid();
            var ii0 = new ItemId(typeof(HeaderContent), id0);
            var dict = new Dictionary<ItemId, string>();
            dict.Add(ii0, "hello");

            string ser = JsonConvert.SerializeObject(dict);
            var dictOut = JsonConvert.DeserializeObject<Dictionary<ItemId, string>>(ser);

            Assert.Equal("hello", dictOut[ii0]);

            ser = JsonConvert.SerializeObject(ii0);
            var iiOut = JsonConvert.DeserializeObject<ItemId>(ser);

            Assert.Equal(ii0, iiOut);
        }
    }
}
