using System;
using System.Linq;
using System.Collections.Generic;
using Lynicon.Collation;
using Lynicon.Repositories;
using Lynicon.Extensibility;
using Lynicon.Relations;
using LyniconANC.Test.Models;
using Xunit;

// Initialise database with test data
//  use ef directly, use appropriate schema for modules in use
// Attach event handlers to run at end of others, handlers store data for checking in class local vars
// 

namespace LyniconANC.Test
{
    [Collection("Lynicon System")]
    public class ReferencesTest
    {
        LyniconSystemFixture sys;

        public ReferencesTest(LyniconSystemFixture sys)
        {
            this.sys = sys;
        }

        [Fact]
        public void ReferenceTest()
        {
            var iid = new ItemId(typeof(RefTargetContent), Guid.NewGuid());
            Reference<RefTargetContent> refr = new Reference<Test.Models.RefTargetContent>(iid);
            Assert.Equal(refr.ItemId, iid);

            iid = null;
            refr = new Reference<RefTargetContent>(iid);
            Assert.True(refr.IsEmpty, "empty reference from null itemid");

            string s = null;
            refr = new Reference<RefTargetContent>(s);
            Assert.True(refr.IsEmpty, "empty reference from null serialization string");

            refr = new Reference<RefTargetContent>(null, null);
            Assert.True(refr.IsEmpty, "empty reference from null id/datatype");

            refr = new Reference<RefTargetContent>(typeof(RefTargetContent).FullName, null);
            Assert.True(refr.IsEmpty, "emtpy referent from null id, valid datatype");

            refr = new Reference<RefTargetContent>();
            Assert.True(refr.IsEmpty, "default constr reference is empty");
        }

        [Fact]
        public void FollowRefs()
        {
            var rt1 = Collator.Instance.GetNew<RefTargetContent>(new Address(typeof(RefTargetContent), "1"));
            var rt2 = Collator.Instance.GetNew<RefTargetContent>(new Address(typeof(RefTargetContent), "2"));
            rt1.Title = "RT1";
            rt2.Title = "RT2";
            rt1.RTString = "xyz";
            Collator.Instance.Set(rt1);
            Collator.Instance.Set(rt2);

            var rc1 = Collator.Instance.GetNew<RefContent>(new Address(typeof(RefContent), "1"));
            var rc2 = Collator.Instance.GetNew<RefContent>(new Address(typeof(RefContent), "2"));
            rc1.Title = "RC1";
            rc2.Title = "RC2";
            rc1.RefTarget = new Reference<RefTargetContent>(rt1.ItemId);
            rc1.RefTargetOther = new Reference<RefTargetContent>(rt2.ItemId);
            rc2.RefTarget = new Reference<RefTargetContent>(rt1.ItemId);
            Collator.Instance.Set(rc1);
            Collator.Instance.Set(rc2);

            var backRefs = Reference.GetReferencesFrom<RefContent>(new ItemVersionedId(rt1.OriginalRecord), "RefTarget").ToList();
            Assert.Equal(2, backRefs.Count);
            backRefs = Reference.GetReferencesFrom<RefContent>(new ItemVersionedId(rt2.OriginalRecord), "RefTarget").ToList();
            Assert.Equal(0, backRefs.Count);
            backRefs = Reference.GetReferencesFrom<RefContent>(new ItemVersionedId(rt2.OriginalRecord), "RefTargetOther").ToList();
            Assert.Equal(1, backRefs.Count);

            rc2.RefTarget = new Reference<RefTargetContent>(rt2.ItemId);
            Collator.Instance.Set(rc2);
            backRefs = Reference.GetReferencesFrom<RefContent>(new ItemVersionedId(rt1.OriginalRecord), "RefTarget").ToList();
            Assert.Equal(1, backRefs.Count);

            Collator.Instance.Delete(rc1);
            backRefs = Reference.GetReferencesFrom<RefContent>(new ItemVersionedId(rt1.OriginalRecord), "RefTarget").ToList();
            Assert.Equal(0, backRefs.Count);
        }
    }
}
