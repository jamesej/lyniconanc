﻿using Lynicon.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Routing
{
    public class MockHttpContext : DefaultHttpContext
    {
        HttpRequest req;
        HttpResponse resp;

        public MockHttpContext()
            : base(new FeatureCollection())
        {
            //Features.Set<IServiceProvidersFeature>(new RequestServicesFeatureMock());
            Features.Set<IHttpRequestFeature>(new HttpRequestFeature());
            Features.Set<IHttpResponseFeature>(new HttpResponseFeature());
            var mockReq = new MockHttpRequest();
            mockReq.SetHttpContext(this);
            req = mockReq;
            resp = new MockHttpResponse(this);
            var serviceContainer = new ServiceContainer();
            RequestServices = serviceContainer;
            serviceContainer.AddService(typeof(ILoggerFactory), new LoggerFactory());
        }
        public MockHttpContext(string url) : this()
        {
            var mockReq = new MockHttpRequest(url);
            mockReq.SetHttpContext(this);
            req = mockReq;
        }

        public override HttpRequest Request
        {
            get
            {
                return req;
            }
        }

        public override HttpResponse Response
        {
            get
            {
                return resp;
            }
        }

        public override IServiceProvider RequestServices { get; set; }
    }
}
