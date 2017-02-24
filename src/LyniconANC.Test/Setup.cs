using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Lynicon.Services;
using LyniconANC.Test.Models;
using Lynicon.Collation;
using Lynicon.Models;
using Lynicon.Repositories;
using Lynicon.Membership;
using LyniconANC.Test.Models;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using LyniconANC.Test;
using Lynicon.Map;
using Lynicon.Modules;
using Lynicon.Extensibility;
using Lynicon.DataSources;

namespace LyniconANC.Test
{
    [SetUpFixture]
    public class Setup
    {
        public static LyniconSystem LyniconSystem { get; private set; }

        public static LyniconSystem LyniconSystemWithDb { get; private set; }

        [OneTimeSetUp]
        public static void GlobalInit()
        {
            LyniconSystem = new LyniconSystem(new LyniconSystemOptions()
                .UseTypeSetup(col =>
                {
                    col.SetupTypeForBasic<TestData>();
                    col.SetupType<HeaderContent>();
                    col.SetupType<HeaderContent2>();
                    col.SetupType<Sub1TContent>();
                    col.SetupType<Sub2TContent>();
                    col.SetupType<RefContent>();
                    col.SetupType<RefTargetContent>();
                    col.SetupType<RestaurantContent>();
                    col.SetupType<ChefContent>();
                    col.SetupType<PathAddressData>();
                    col.SetupType<SplitAddressData>();
                    col.SetupType<PropertyRedirectContent>();
                    col.Repository.Register(null, new ContentRepository(new MockDataSourceFactory()));
                    col.Repository.Register(typeof(TestData), new BasicRepository(new MockDataSourceFactory()));
                    col.Repository.Register(typeof(ContentItem), new ContentRepository(new MockDataSourceFactory()));
                }));

            LyniconSystem.Construct(new Module[] { new CoreModule() });
            LyniconSystem.Modules.SkipDbStateCheck = true;
            LyniconSystem.Initialise();

            //SetupLyniconSystemWithDb();

            VersionManager.Instance.RegisterVersion(new TestVersioner());

            var testingRoutes = new RouteCollection();
            testingRoutes.AddTestDataRoute<HeaderContent>("header", "header/{_0}", new { controller = "mock", action = "mock" });
            testingRoutes.AddTestDataRoute<TestData>("test-data", "testd/{_0}", new { controller = "mock", action = "mock" });
            testingRoutes.AddTestDataRoute<ChefContent>("chef", "header/{_0}", new { controller = "mock", action = "mock" });
            testingRoutes.AddTestDataRoute<HeaderContent2>("hc2", "header2", new { controller = "mock", action = "mock" });
            testingRoutes.AddTestDataRoute<RefContent>("ref", "ref/{_0}/{_1}", new { controller = "mock", action = "mock" });
            ContentMap.Instance.RouteCollection = testingRoutes;
        }

        private static void SetupLyniconSystemWithDb()
        {
            LyniconSystemWithDb = new LyniconSystem(new LyniconSystemOptions()
                .UseConnectionString("Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog=LynTest;Integrated Security=True")
                .UseTypeSetup(col =>
                {
                    col.SetupTypeForBasic<TestData>();
                    col.SetupType<HeaderContent>();
                    col.Repository.Register(null, new ContentRepository(new CoreDataSourceFactory()));
                    col.Repository.Register(typeof(TestData), new BasicRepository(new CoreDataSourceFactory()));
                    col.Repository.Register(typeof(ContentItem), new ContentRepository(new CoreDataSourceFactory()));
                }));

            LyniconSystemWithDb.Collator = new Collator(new Repository());
            LyniconSystemWithDb.Modules = new LyniconModuleManager();

            LyniconSystemWithDb.Construct(new Module[] { new CoreModule() });
            LyniconSystemWithDb.Modules.SkipDbStateCheck = true;
            LyniconSystemWithDb.Initialise();
        }
    }
}
