using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Lynicon.Logging
{
    public static class Log4NetExtensions
    {
        public static void ConfigureLog4Net(this IHostingEnvironment env, string configFileRelativePath)
        {
            GlobalContext.Properties["appRoot"] = env.ContentRootPath;
            XmlConfigurator.Configure(new FileInfo(Path.Combine(env.ContentRootPath, configFileRelativePath)));
        }

        public static void AddLog4Net(this ILoggerFactory loggerFactory)
        {
            loggerFactory.AddProvider(new Log4NetProvider());
        }
    }
}
