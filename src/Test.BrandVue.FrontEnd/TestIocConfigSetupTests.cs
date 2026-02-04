using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using Autofac;
using BrandVue;
using BrandVue.EntityFramework;
using BrandVue.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.DataWarehouseTests;
using Test.BrandVue.FrontEnd.Mocks;
using Test.BrandVue.FrontEnd.SurveyApi.Endpoints;
using Vue.Common.Auth.Permissions;

namespace Test.BrandVue.FrontEnd
{
    class TestIocConfigSetupTests
    {
        private const string Message = "These services will fail to resolve at runtime unless the Startup maps them outside the IoCConfig";
        private static readonly Type ProductionIocConfigType = typeof(IoCConfig);
        private static readonly ILoggerFactory LoggerFactory = Substitute.For<ILoggerFactory>();
        private static readonly AppSettings AppSettings = new AppSettings(appSettingsCollection: CreateCommonAppSettings());

        private static IEnumerable<IoCConfig> OtherIocConfigs { get; } = new IoCConfig[]
        {
            new BrandVueApiTestIoCConfig(LoggerFactory, AppSettings,
                new ProductContext(AppSettings.ProductToLoadDataFor), 
                Substitute.For<IOptions<MixPanelSettings>>(),
                Substitute.For<IOptions<ProductSettings>>(),
                new ConfigurationManager()),
            new DataWarehouseIoCConfig(AppSettings, LoggerFactory, new BrandVueProductToTest(AppSettings.ProductToLoadDataFor), "demo", Substitute.For<IOptions<MixPanelSettings>>(),
                Substitute.For<IOptions<ProductSettings>>()),
            new TestServerIocTests.ConstructionOnlyIocConfig(
                AppSettings,
                NullLoggerFactory.Instance,
                Substitute.For<IOptions<MixPanelSettings>>(),
                Substitute.For<IOptions<ProductSettings>>()),
        };


        private static NameValueCollection CreateCommonAppSettings()
        {
            var collection = AppSettings.ReadFromAppSettingsJson(Environment.CurrentDirectory);

            new Dictionary<string, string>
                {
                    {"productsToLoadDataFor", "barometer"},
                }.ToList().ForEach(kvp => collection[kvp.Key] = kvp.Value);
            return collection;
        }

        [Test]
        public void AllTestIocConfigsAreTested()
        {
            var testedTypes = OtherIocConfigs.Select(c => c.GetType());
            var testIocConfigsInThisAssembly = typeof(BrandVueApiTestIoCConfig).Assembly.GetTypes().Where(t => ProductionIocConfigType.IsAssignableFrom(t));
            Assert.That(testedTypes, Is.EquivalentTo(testIocConfigsInThisAssembly));
        }

        [Test, TestCaseSource(nameof(OtherIocConfigs))]
        public void IoCConfigsOnlyBindThingsBoundInProduction(IoCConfig testIocConfig)
        {
            var servicesBoundForTests = GetRegistrationDescriptions(testIocConfig).ToList();
            var servicesBoundForProduction = GetRegistrationDescriptions(new IoCConfig(
                AppSettings, 
                LoggerFactory, 
                Substitute.For<IOptions<MixPanelSettings>>(),
                Substitute.For<IOptions<ProductSettings>>(),
                new ConfigurationManager()));
            var servicesOnlyBoundInTests = servicesBoundForTests.Except(servicesBoundForProduction);
            Assert.That(servicesOnlyBoundInTests.Where(t => !t.EndsWith("Controller") && !t.EndsWith("ICalculationLogger")), Is.Empty, Message);
        }

        private static IEnumerable<string> GetRegistrationDescriptions(IoCConfig ioCConfig)
        {
            var registrations = GetRegistrations(ioCConfig);
            return registrations.Select(x => x.Description);
        }

        private static IEnumerable<Autofac.Core.Service> GetRegistrations(IoCConfig ioCConfig)
        {
            var container = CreateContainer(ioCConfig);
            var registrations = container
                .ComponentRegistry.Registrations.SelectMany(x => x.Services);
            return registrations;
        }

        private static IContainer CreateContainer(IoCConfig ioCConfig)
        {
            var builder = new ContainerBuilder();
            //This is bound in Startup with AddHttpClient()
            builder.Register(c => Substitute.For<IHttpClientFactory>()).As<IHttpClientFactory>();
            var userPermissionHttpClient = Substitute.For<IUserPermissionHttpClient>();
            userPermissionHttpClient.GetUserFeaturePermissionsAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new List<PermissionFeatureOptionDto>());
            //This is bound in Program.cs with AddScoped()
            builder.Register(c => userPermissionHttpClient).As<IUserPermissionHttpClient>();

            var container = ioCConfig.Register(builder).Build();
            return container;
        }
    }
}