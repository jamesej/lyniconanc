using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Extensibility
{
    public class RequestContextManager
    {
        public static RequestContextManager Instance { get; set; }

        static RequestContextManager()
        {
            Instance = new RequestContextManager(null);
        }

        private readonly IHttpContextAccessor contextAccessor;
        private IServiceProvider initialScopeServiceProvider;

        public RequestContextManager(IServiceProvider prov)
        {
            this.initialScopeServiceProvider = prov;
            if (prov != null)
                this.contextAccessor = (IHttpContextAccessor)prov.GetService(typeof(IHttpContextAccessor));
        }

        public HttpContext CurrentContext
        {
            get
            {
                if (contextAccessor == null)
                    return null;
                return contextAccessor.HttpContext;
            }
        }

        public IServiceProvider ScopedServiceProvider
        {
            get
            {
                if (contextAccessor == null || contextAccessor.HttpContext == null)
                    return this.initialScopeServiceProvider;
                else
                    return contextAccessor.HttpContext.RequestServices;
            }
        }
    }
}
