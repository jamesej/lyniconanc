using System;
using System.Linq;
using System.Collections.Generic;
using Lynicon.Collation;
using Lynicon.Repositories;
using Lynicon.Extensibility;
using Lynicon.Relations;
using LyniconANC.Test.Models;
using Lynicon.Exceptions;
using Xunit;
using Lynicon.Map;

// Initialise database with test data
//  use ef directly, use appropriate schema for modules in use
// Attach event handlers to run at end of others, handlers store data for checking in class local vars
// 

namespace LyniconANC.Test
{
    [Collection("Lynicon System")]
    public class UrlTest
    {
        LyniconSystemFixture sys;

        public UrlTest(LyniconSystemFixture sys)
        {
            this.sys = sys;
        }

        [Fact]
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
            catch (ProhibitedActionException appEx)
            {
                ex = appEx;
            }
            Assert.NotNull(ex);

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
            //Assert.NotNull(ex, "Failed to block move to an existing standard url");
        }

        [Fact]
        public void AddressOccupied()
        {
            Address r1addr = new Address(typeof(RestaurantContent), "");
            bool occupied = ContentMap.Instance.AddressOccupied(r1addr);
            Assert.False(occupied);

            var r1 = Collator.Instance.GetNew<RestaurantContent>(r1addr);

            r1.Title = "r1";
            Collator.Instance.Set(r1);

            occupied = ContentMap.Instance.AddressOccupied(r1addr);
            Assert.True(occupied);
        }
    }
}
