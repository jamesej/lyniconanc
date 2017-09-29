using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

using Lynicon.Commands;

namespace LyniconANC.Release
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(ContentRootLocator.GetContentRoot(args) ?? Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .Build();

            if (Lynicon.Commands.CommandRunner.InterceptAndRunCommands(host.Services, args))
                return;

            host.Run();
        }
    }
}
