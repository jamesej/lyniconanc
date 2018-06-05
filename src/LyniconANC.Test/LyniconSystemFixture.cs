using Lynicon.Services;
using LyniconANC.Test.Models;
using Lynicon.Collation;
using Lynicon.Repositories;
using Microsoft.AspNetCore.Routing;
using Lynicon.Map;
using Lynicon.Modules;
using Lynicon.Extensibility;
using Lynicon.DataSources;
using Xunit;
using Lynicon.Models;
using System.Reflection;
using System.Collections.Generic;

namespace LyniconANC.Test
{
    [CollectionDefinition("Lynicon System")]
    public class LyniconSystemCollectionFixture : ICollectionFixture<LyniconSystemFixture>
    { }

    public class LyniconSystemFixture
    {
        public LyniconSystem LyniconSystem { get; private set; }

        public LyniconSystem LyniconSystemWithDb { get; private set; }

        public LyniconSystemFixture()
        {
            ContentTypeHierarchy.RegisterControllersFromAssemblies(new List<Assembly> { this.GetType().GetTypeInfo().Assembly });
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
                    col.SetupTypeForBasic<RedirectData>();
                    col.SetupType<RedirectTargetContent>();
                    col.System.Repository.Register(null, new ContentRepository(col.System, new MockDataSourceFactory(col.System)));
                    col.System.Repository.Register(typeof(TestData), new BasicRepository(col.System, new MockDataSourceFactory(col.System)));
                    col.System.Repository.Register(typeof(RedirectData), new BasicRepository(col.System, new MockDataSourceFactory(col.System)));
                    col.System.Repository.Register(typeof(ContentItem), new ContentRepository(col.System, new MockDataSourceFactory(col.System)));
                }));

            LyniconSystem.Extender.AddExtensionRule(typeof(TestData), typeof(IExtTestData));
            LyniconSystem.Extender.AddExtensionRule(typeof(ICoreMetadata), typeof(IPublishable));
            LyniconSystem.Extender.AddExtensionRule(typeof(ICoreMetadata), typeof(IInternational));

            LyniconSystem.Construct(new Lynicon.Extensibility.Module[] { new CoreModule(LyniconSystem) });
            LyniconSystem.Modules.SkipDbStateCheck = true;
            LyniconSystem.SetAsPrimarySystem();
            LyniconSystem.Initialise();

            //SetupLyniconSystemWithDb();

            //VersionManager.Instance.RegisterVersion(new TestVersioner());

            var testingRoutes = new RouteCollection();
            testingRoutes.AddTestDataRoute<HeaderContent>("header", "header/{_0}", new { controller = "mock", action = "mock" });
            testingRoutes.AddTestDataRoute<TestData>("test-data", "testd/{_0}", new { controller = "mock", action = "mock" });
            testingRoutes.AddTestDataRoute<ChefContent>("chef", "header/{_0}", new { controller = "mock", action = "mock" });
            testingRoutes.AddTestDataRoute<HeaderContent2>("hc2", "header2", new { controller = "mock", action = "mock" });
            testingRoutes.AddTestDataRoute<RefContent>("ref", "ref/{_0}/{_1}", new { controller = "mock", action = "mock" });
            testingRoutes.AddTestDataRoute<HeaderContent>("header-write", "header-write/{_0}", new { controller = "mock", action = "mock" }, new ContentPermission((roles, data) => true));
            ContentMap.Instance.RouteCollection = testingRoutes;

            VersionManager.Instance.RegisterVersion(new PublishingVersioner(LyniconSystem, t => t == typeof(HeaderContent)));
            VersionManager.Instance.RegisterVersion(new I18nVersioner(LyniconSystem, new string[] { "en-GB", "es-ES" }, "locale", "en-GB", s => s));

            Collator.Instance.SetupType<TestContent>(new TestCollator(), null);
        }

        private void SetupLyniconSystemWithDb()
        {
            LyniconSystemWithDb = new LyniconSystem(new LyniconSystemOptions()
                .UseConnectionString("Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog=LynTest;Integrated Security=True")
                .UseTypeSetup(col =>
                {
                    col.SetupTypeForBasic<TestData>();
                    col.SetupType<HeaderContent>();
                    col.System.Repository.Register(null, new ContentRepository(new CoreDataSourceFactory(col.System)));
                    col.System.Repository.Register(typeof(TestData), new BasicRepository(col.System, new CoreDataSourceFactory(col.System)));
                    col.System.Repository.Register(typeof(ContentItem), new ContentRepository(new CoreDataSourceFactory(col.System)));
                }));

            LyniconSystemWithDb.Collator = new Collator(LyniconSystemWithDb);
            LyniconSystemWithDb.Modules = new LyniconModuleManager();

            LyniconSystemWithDb.Construct(new Lynicon.Extensibility.Module[] { new CoreModule(LyniconSystemWithDb) });
            LyniconSystemWithDb.Modules.SkipDbStateCheck = true;
            LyniconSystemWithDb.Initialise();
        }
    }
}
