using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Utility
{
    public static class HttpX
    {
        public static string GetCookie(this HttpResponse resp, string key)
        {
            return resp.Headers[HeaderNames.SetCookie].FirstOrDefault(s => s.StartsWith(key + "="));
        }
    }
}
