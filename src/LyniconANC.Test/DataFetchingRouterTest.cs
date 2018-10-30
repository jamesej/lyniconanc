using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Routing;
using LyniconANC.Test.Models;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace LyniconANC.Test
{
    [Collection("Lynicon System")]
    public class DataFetchingRouterTest
    {
        LyniconSystemFixture sys;

        public DataFetchingRouterTest(LyniconSystemFixture sys)
        {
            this.sys = sys;
        }

        [Fact]
        public void SiteUserRequestExisting()
        {
            var hc = Collator.Instance.GetNew<HeaderContent2>(new Address(typeof(HeaderContent2), "dfr-a"));
            hc.Title = "DFR Header A";
            Collator.Instance.Set(hc);

            // Anon user content on route

            TestEditorScenario("dfr-a", "Header2");

            // Anon user json

            TestEditorScenario("dfr-a", "Api", jsonRequest: true);

            // Anon user no content on route

            TestEditorScenario("dfr-b", null);

            // Editor user content on route

            TestEditorScenario("dfr-a", "DualFrameEditor", true);

            // Editor user content on route, $mode=view

            TestEditorScenario("dfr-a", "Header2", true, modeView: true);

            // Editor user no content on route, $type specified

            TestEditorScenario("dfr-a", "DualFrameEditor", true, true);

            // Editor user no content on route, $type not specified

            TestEditorScenario("dfr-b", null, true);
        }

        public void TestEditorScenario(string path, string controllerExpected, bool editor = false, bool typespecified = false, bool modeView = false, bool jsonRequest = false)
        {
            var routeContext = new RouteContext(new MockHttpContext("http://www.test.com/header2/" + path
                + (typespecified ? "?$type=LyniconANC.Test.Models.HeaderContent2" : "")
                + (modeView ? "?$mode=view" : "")));
            var rd = new RouteData();
            rd.Values.Add("_0", path);
            rd.Values.Add("controller", "Header2");
            rd.Values.Add("action", "Index");
            routeContext.RouteData = rd;
            if (jsonRequest)
                routeContext.HttpContext.Request.Headers.Add("Accept", new StringValues("application/json"));
            var router = new DataFetchingRouter<HeaderContent2>(new MockRouter(),
                writePermission: (editor
                    ? new ContentPermission((roles, data) => true)
                    : null));
            router.RouteAsync(routeContext).Wait();

            if (controllerExpected == null)
            {
                Assert.Null(routeContext.Handler);
            }
            else
            {
                Assert.NotNull(routeContext.Handler);
                Assert.Equal(controllerExpected, routeContext.RouteData.Values["controller"]);
            }
        }
    }
}
