using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Http.Internal;
using Lynicon.Utility;
using Microsoft.Extensions.Primitives;

namespace Lynicon.Routing
{
    public class MockHttpRequest : HttpRequest
    {
        public MockHttpRequest() : base()
        {
            Form = new FormCollection(new Dictionary<string, StringValues>());
            headers = new HeaderDictionary();
            Cookies = new RequestCookieCollection();
            Method = "GET";
            Body = new MemoryStream();
            ContentLength = 0;
        }
        public MockHttpRequest(string url) : this()
        {
            var uri = new Uri(url);
            this.Path = uri.AbsolutePath;
            this.PathBase = "";
            this.QueryString = new QueryString(uri.Query);
            this.Host = new HostString(uri.Host);
            this.IsHttps = uri.Scheme == "https";
            this.Scheme = uri.Scheme;
        }

        public override Stream Body { get; set; }

        public override long? ContentLength { get; set; }

        public override string ContentType { get; set; }

        public override IRequestCookieCollection Cookies { get; set; }

        public override IFormCollection Form { get; set; }

        public override bool HasFormContentType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected IHeaderDictionary headers;
        public override IHeaderDictionary Headers { get { return headers; } }

        public override HostString Host { get; set; }

        protected HttpContext httpContext;
        public override HttpContext HttpContext { get { return httpContext; } }

        public override bool IsHttps { get; set; }

        public override string Method { get; set; }

        public override PathString Path { get; set; }

        public override PathString PathBase { get; set; }

        public override string Protocol { get; set; }

        IQueryCollection query;
        public override IQueryCollection Query
        {
            get
            {
                return query;
            }

            set
            {
                query = value;
            }
        }

        QueryString queryString;
        public override QueryString QueryString
        {
            get
            {
                return queryString;
            }

            set
            {
                queryString = value;
                string queryPart = queryString.ToString();
                queryPart = queryPart.StartsWith("?") ? queryPart.After("?") : queryPart;
                if (string.IsNullOrEmpty(queryPart))
                    Query = new QueryCollection();
                else
                {
                    var qDict = queryPart.Split('&').ToDictionary(s => s.UpTo("="), s => new StringValues(s.After("=")));
                    Query = new QueryCollection(qDict);
                }
            }
        }

        public override string Scheme { get; set; }

        public override Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public void SetHttpContext(HttpContext httpContext)
        {
            this.httpContext = httpContext;
        }
    }
}
