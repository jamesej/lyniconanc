using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Routing
{
    public class MockHttpResponse : DefaultHttpResponse
    {
        public MockHttpResponse(HttpContext ctx) : base(ctx)
        { }
    }
}
