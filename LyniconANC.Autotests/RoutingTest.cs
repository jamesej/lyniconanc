using Lynicon.Collation;
using Lynicon.Map;
using Lynicon.Routing;
using LyniconANC.Test.Models;
using Microsoft.AspNetCore.Routing;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyniconANC.Autotests
{
    [TestFixture]
    public class RoutingTest
    {
        [Test]
        public void DataRouter()
        {
            var hc = Collator.Instance.GetNew<HeaderContent>(new Address(typeof(HeaderContent), "pqp"));
            hc.Title = "PQP";
            Collator.Instance.Set(hc);
            var rc = new RouteContext(new MockHttpContext("http://www.test.com/header/pqp"));
            ContentMap.Instance.RouteCollection.RouteAsync(rc).Wait();
            Route r0 = rc.RouteData.Routers[0] as Route;
            Assert.IsNotNull(rc.Handler);
            Assert.AreEqual("header/{_0}", r0.RouteTemplate);
            Assert.AreEqual("pqp", rc.RouteData.Values["_0"]);
            Assert.AreEqual(typeof(HeaderContent), rc.RouteData.Values["data"].GetType());

            var chf = Collator.Instance.GetNew<ChefContent>(new Address(typeof(ChefContent), "zyz"));
            chf.PageTitle = "ZYZ";
            Collator.Instance.Set(chf);
            rc = new RouteContext(new MockHttpContext("http://www.test.com/header/zyz"));
            ContentMap.Instance.RouteCollection.RouteAsync(rc).Wait();
            r0 = rc.RouteData.Routers[0] as Route;
            Assert.IsNotNull(rc.Handler);
            Assert.AreEqual("header/{_0}", r0.RouteTemplate);
            Assert.AreEqual("zyz", rc.RouteData.Values["_0"]);
            Assert.AreEqual(typeof(ChefContent), rc.RouteData.Values["data"].GetType());

            var h2 = Collator.Instance.GetNew<HeaderContent2>(new Address(typeof(HeaderContent2), ""));
            h2.Title = "MNM";
            Collator.Instance.Set(h2);
            rc = new RouteContext(new MockHttpContext("http://www.test.com/header2"));
            ContentMap.Instance.RouteCollection.RouteAsync(rc).Wait();
            r0 = rc.RouteData.Routers[0] as Route;
            Assert.IsNotNull(rc.Handler);
            Assert.AreEqual("header2", r0.RouteTemplate);
            Assert.AreEqual(typeof(HeaderContent2), rc.RouteData.Values["data"].GetType());

            var rfc = Collator.Instance.GetNew<RefContent>(new Address(typeof(RefContent), "ab&cd"));
            rfc.Title = "AB.CD";
            Collator.Instance.Set(rfc);
            rc = new RouteContext(new MockHttpContext("http://www.test.com/ref/ab/cd"));
            ContentMap.Instance.RouteCollection.RouteAsync(rc).Wait();
            r0 = rc.RouteData.Routers[0] as Route;
            Assert.IsNotNull(rc.Handler);
            Assert.AreEqual("ref/{_0}/{_1}", r0.RouteTemplate);
            Assert.AreEqual(typeof(RefContent), rc.RouteData.Values["data"].GetType());

            rc = new RouteContext(new MockHttpContext("http://www.test.com/ref/ab/dd"));
            ContentMap.Instance.RouteCollection.RouteAsync(rc).Wait();
            Assert.AreEqual(0, rc.RouteData.Routers.Count);

            rc = new RouteContext(new MockHttpContext("http://www.test.com/header2/dd"));
            ContentMap.Instance.RouteCollection.RouteAsync(rc).Wait();
            Assert.AreEqual(0, rc.RouteData.Routers.Count);
        }
    }
}
