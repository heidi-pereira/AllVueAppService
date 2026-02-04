using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Autofac;
using BrandVue.EntityFramework;
using BrandVue.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using Savanta.Logging;
using Vue.AuthMiddleware.Api;
using Vue.Common.Auth.Permissions;
using BrandVue.SourceData.Snowflake;
using ZiggyCreatures.Caching.Fusion;
using Vue.Common.Auth;

namespace Test.BrandVue.FrontEnd.DataWarehouseTests
{
    public abstract record ProductToTest(string ProductName, string SubProduct)
    {
        public const string AllVueProductName = "survey";
        public bool IsAllVue => ProductName == AllVueProductName;
        public override string ToString()
        {
            if (string.IsNullOrEmpty(SubProduct))
                return ProductName;
            return $"{ProductName}\\{SubProduct}";
        }
    }

    public record BrandVueProductToTest(string ProductName) : ProductToTest(ProductName, null)
    {
        public override string ToString()
        {
            return base.ToString();
        }
    }

    public record AllVueProductToTest(string SurveyName) : ProductToTest(AllVueProductName, SurveyName)
    {
        public override string ToString()
        {
            return base.ToString();
        }

    }

    internal class LocalBrandVueServer
    {

        private static readonly ConcurrentDictionary<ProductToTest, LocalBrandVueServer> PerProductCache = new();

        public static LocalBrandVueServer For(ProductToTest productToTest) => PerProductCache.GetOrAdd(productToTest,
            _ => new LocalBrandVueServer(productToTest));

        private readonly AppSettings _appSettings;
        private readonly Lazy<ILifetimeScope> _lazyLifetimeScope;
        public ProductToTest ProductToTest { get; }

        private LocalBrandVueServer(ProductToTest productToTest)
        {
            _appSettings = CreateAppSettings(productToTest);
            ProductToTest = productToTest;//_appSettings.ProductToLoadDataFor;
            _lazyLifetimeScope = new Lazy<ILifetimeScope>(GetScope);
        }
        
        public ILifetimeScope LifetimeScope => _lazyLifetimeScope.Value;

        private AppSettings CreateAppSettings(ProductToTest productToTest)
        {
            var appSettings = new AppSettings(overrideAppSettingsJson: "appSettings.datawarehousetests.json");

            // Azure Piplines can't have . in the Environment Variables - so switching to _ instead
            string answersConnectionString = Environment.GetEnvironmentVariable("DATAWAREHOUSETESTS_ANSWERSCONNECTIONSTRING");
            string metaConnectionString = Environment.GetEnvironmentVariable("DATAWAREHOUSETESTS_METACONNECTIONSTRING");
            if (answersConnectionString != null) appSettings.AppSettingsCollection["AnswersConnectionString"] = answersConnectionString;
            if (metaConnectionString != null) appSettings.AppSettingsCollection["MetaConnectionString"] = metaConnectionString;
            appSettings.AppSettingsCollection["ProductsToLoadDataFor"] = productToTest.ProductName;
            if (productToTest.IsAllVue)
            {
                appSettings.AppSettingsCollection["loadConfigFromSql"] = true.ToString();
                appSettings.AppSettingsCollection["useSubProductPathPrefix"] = true.ToString();
            }
            return new AppSettings(appSettingsCollection: appSettings.AppSettingsCollection); // Connection string is read in constructor, so recreate
        }

        public HttpClient CreateBrandVueApiClient()
        {
            var targetEnvironment = _appSettings.AppDeploymentEnvironment;
            var baseUri = targetEnvironment switch
            {

                "live" => $"https://demo.all-vue.com/{ProductToTest}",
                "beta" => $"https://demo.beta.all-vue.com/{ProductToTest}",
                "test" => $"https://demo.test.all-vue.com/{ProductToTest}",
                _ => "http://localhost:8082"
            };
            var apiKey = Environment.GetEnvironmentVariable("DATAWAREHOUSETESTS_TARGETAPIKEY") ??
                      (targetEnvironment == AppSettings.DevEnvironmentName
                          ? ApiKeyConstants.DebugApiKey
                          : _appSettings.GetGlobalSetting<string>("TargetApiKey"));

            var client = new HttpClient { BaseAddress = new Uri($"{baseUri}/api/"), Timeout = TimeSpan.FromSeconds(60)};
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            return client;
        }

        private ILifetimeScope GetScope()    
        {
            try
            {
                var builderConfig = new DataWarehouseIoCConfig(
                    _appSettings,
                    SavantaLogging.CreateFactory(),
                    ProductToTest,
                    "demo",
                    Substitute.For<IOptions<MixPanelSettings>>(),
                    Substitute.For<IOptions<ProductSettings>>()
                    );

                var containerBuilder = new ContainerBuilder();
                //This is registered in Startup with AddHttpClient()
                containerBuilder.Register(c => Substitute.For<IHttpClientFactory>()).As<IHttpClientFactory>();
                containerBuilder.Register(_ => Substitute.For<IConfiguration>()).As<IConfiguration>();

                //This is bound in Program.cs with AddScoped()
                var userPermissionHttpClient = Substitute.For<IUserPermissionHttpClient>();
                userPermissionHttpClient.GetUserFeaturePermissionsAsync(Arg.Any<string>(), Arg.Any<string>())
                    .Returns(new List<PermissionFeatureOptionDto>());
                containerBuilder.Register(c => userPermissionHttpClient).As<IUserPermissionHttpClient>();

                //This is bound in Startup.cs with AddSingleton()
                containerBuilder.Register(c => Substitute.For<ISnowflakeRepository>()).As<ISnowflakeRepository>().SingleInstance();

                //This is bound in Startup.cs with AddFusionCache()
                containerBuilder.Register(c => Substitute.For<IFusionCacheProvider>()).As<IFusionCacheProvider>();

                builderConfig.Register(containerBuilder);

                return containerBuilder.Build().BeginLifetimeScope();
            }
            catch (Exception e)
            {
                Assert.Ignore($"These tests currently require real metadata and data to be present: {e}");
                return null;
            }
        }
    }
}
