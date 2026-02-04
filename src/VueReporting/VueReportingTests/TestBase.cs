using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using VueReporting;
using VueReporting.Services;

namespace VueReportingTests
{
    public abstract class TestBase
    {
        protected ServiceProvider ServiceProvider;

        [SetUp]
        public void SetUp()
        {
            var basePath = TestContext.CurrentContext.TestDirectory;

            var config = new ConfigurationBuilder().SetBasePath(basePath)
                .AddJsonFile("appsettings.json", true)
                .Build();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(s => config);

            var substituteLogFactory = Substitute.For<ILoggerFactory>();

            Startup.ConfigureIoC(serviceCollection, config, substituteLogFactory, o => o.UseInMemoryDatabase("Test"));

            // Override test-specific services
            serviceCollection.Replace(ServiceDescriptor.Singleton<IBrandVueService, TestChartExporterService>());
            serviceCollection.Replace(ServiceDescriptor.Singleton<IAppSettings, TestAppSettings>());

            ServiceProvider = serviceCollection.BuildServiceProvider();
        }
    }

    public class TestAppSettings: IAppSettings

    {
        public string Root { get; }
        public string ProductName { get; }
        public string UserName { get; }
        public string ProductFilter { get; }
        public string ProductDescription { get; }
        public string[] ExcludedFilters { get; set; } = new string[0];
        public string[] RemoveFilters { get; } = new string[0];
        public string ReportingApiAccessToken { get; set; }
        public string BrandVueMainBrandParameter { get; }
        public string BrandVueKeyCompetitorParameter { get; }
        public string BrandVueCompetitorParameter { get; }
        public string BrandVueSubsetParameter { get; }
    }
}