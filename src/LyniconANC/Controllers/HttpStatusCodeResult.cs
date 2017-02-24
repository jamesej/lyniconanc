using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.IO;

namespace Lynicon.Controllers
{
    /// <summary>
    /// ActionResult which is an HTTP status code with no body
    /// </summary>
    public class HttpStatusCodeResult : IActionResult
    {
        private readonly int code;
        private readonly string description;
        public HttpStatusCodeResult(int code, string description)
        {
            this.code = code;
            this.description = description;
        }
        public HttpStatusCodeResult(int code)
            : this(code, "")
        { }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = code;
            if (500 <= code && code < 600)
            {
                context.HttpContext.Response.ContentType = "text/plain";
                using (var writer = new StreamWriter(context.HttpContext.Response.Body))
                {
                    await writer.WriteAsync(description).ConfigureAwait(false);
                }
            }
            return;
        }
    }
}
