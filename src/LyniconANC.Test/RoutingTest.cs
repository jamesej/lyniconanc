using Lynicon.Collation;
using Lynicon.Map;
using Lynicon.Routing;
using Lynicon.Utility;
using LyniconANC.Test.Models;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LyniconANC.Test
{
    [Collection("Lynicon System")]
    public class RoutingTest
    {
        LyniconSystemFixture sys;

        public RoutingTest(LyniconSystemFixture sys)
        {
            this.sys = sys;
        }

        [Fact]
        public void DataRouter()
        {
            var hc = Collator.Instance.GetNew<HeaderContent>(new Address(typeof(HeaderContent), "pqp"));
            hc.Title = "PQP";
            Collator.Instance.Set(hc);
            var rc = new RouteContext(new MockHttpContext("http://www.test.com/header/pqp"));
            ContentMap.Instance.RouteCollection.RouteAsync(rc).Wait();
            Route r0 = rc.RouteData.Routers[0] as Route;
            Assert.NotNull(rc.Handler);
            Assert.Equal("header/{_0}", r0.RouteTemplate);
            Assert.Equal("pqp", rc.RouteData.Values["_0"]);
            Assert.Equal(typeof(HeaderContent), rc.RouteData.Values["data"].GetType().UnextendedType());

            var chf = Collator.Instance.GetNew<ChefContent>(new Address(typeof(ChefContent), "zyz"));
            chf.PageTitle = "ZYZ";
            Collator.Instance.Set(chf);
            rc = new RouteContext(new MockHttpContext("http://www.test.com/header/zyz"));
            ContentMap.Instance.RouteCollection.RouteAsync(rc).Wait();
            r0 = rc.RouteData.Routers[0] as Route;
            Assert.NotNull(rc.Handler);
            Assert.Equal("header/{_0}", r0.RouteTemplate);
            Assert.Equal("zyz", rc.RouteData.Values["_0"]);
            Assert.Equal(typeof(ChefContent), rc.RouteData.Values["data"].GetType().UnextendedType());

            var h2 = Collator.Instance.GetNew<HeaderContent2>(new Address(typeof(HeaderContent2), ""));
            h2.Title = "MNM";
            Collator.Instance.Set(h2);
            rc = new RouteContext(new MockHttpContext("http://www.test.com/header2"));
            ContentMap.Instance.RouteCollection.RouteAsync(rc).Wait();
            r0 = rc.RouteData.Routers[0] as Route;
            Assert.NotNull(rc.Handler);
            Assert.Equal("header2", r0.RouteTemplate);
            Assert.Equal(typeof(HeaderContent2), rc.RouteData.Values["data"].GetType().UnextendedType());

            var rfc = Collator.Instance.GetNew<RefContent>(new Address(typeof(RefContent), "ab&cd"));
            rfc.Title = "AB.CD";
            Collator.Instance.Set(rfc);
            rc = new RouteContext(new MockHttpContext("http://www.test.com/ref/ab/cd"));
            ContentMap.Instance.RouteCollection.RouteAsync(rc).Wait();
            r0 = rc.RouteData.Routers[0] as Route;
            Assert.NotNull(rc.Handler);
            Assert.Equal("ref/{_0}/{_1}", r0.RouteTemplate);
            Assert.Equal(typeof(RefContent), rc.RouteData.Values["data"].GetType().UnextendedType());

            rc = new RouteContext(new MockHttpContext("http://www.test.com/ref/ab/dd"));
            ContentMap.Instance.RouteCollection.RouteAsync(rc).Wait();
            Assert.Equal(0, rc.RouteData.Routers.Count);

            rc = new RouteContext(new MockHttpContext("http://www.test.com/header2/dd"));
            ContentMap.Instance.RouteCollection.RouteAsync(rc).Wait();
            Assert.Equal(0, rc.RouteData.Routers.Count);
        }
    }
}
