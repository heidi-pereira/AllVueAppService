using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Autofac;
using BrandVue;
using BrandVue.Controllers.Api;
using BrandVue.EntityFramework;
using BrandVue.PublicApi;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;
using Vue.AuthMiddleware;
using Vue.AuthMiddleware.Local;
using TestCommon.Extensions;
using BrandVue.MixPanel;
using Mixpanel;
using static BrandVue.MixPanel.MixPanel;
using Microsoft.Extensions.Configuration;
using BrandVue.Settings;
using Microsoft.Extensions.Options;
using Vue.Common.Constants.Constants;

namespace Test.BrandVue.FrontEnd
{
    public class BrandVueTestServer
    {
        private static readonly IReadOnlySet<string> PublicApiClaimTypes =
            new HashSet<string> { RequiredClaims.Products, RequiredClaims.Subsets, RequiredClaims.CurrentCompanyShortCode, OptionalClaims.BrandVueApi };

        /// <summary>
        /// Uses the valid local debug API key and unwraps ApiResponse by default (call .With to customise HttpClient)
        /// PERF: This is cached for performance reasons. This means all "With..." methods below must return a new object.
        /// </summary>
        public static BrandVueTestServer PublicSurveyApi { get; } = new BrandVueTestServer(new ApiTestConfig(PublicApiConstants.ApiRoot)).WithOnlyTheseClaimTypes(PublicApiClaimTypes);

        /// <summary>
        /// Uses a valid bearer token by default (call .With to customise HttpClient)
        /// PERF: This is cached for performance reasons. This means all "With..." methods below must return a new object.
        /// </summary>
        public static BrandVueTestServer InternalBrandVueApi { get; } = new BrandVueTestServer(CreateDefaultApiTestConfig());

        public static BrandVueTestServer InternalBrandVueApiWithContext(ProductContext context)
        {
            return new BrandVueTestServer(new ApiTestConfig($"/{DataController.Route}"), context);
        }
        private NameValueCollection CreateCommonAppSettings()
        {
            var collection = AppSettings.ReadFromAppSettingsJson(Environment.CurrentDirectory, "appsettings.override.json");

            _appSettingOverrides.ToList().ForEach(kvp => collection[kvp.Key] = kvp.Value);
            return collection;
        }
        private readonly ProductContext _productContext;
        private readonly ApiTestConfig _apiTestConfig;
        private TestServer _testServerBackingField;
        private Action<LocalAuthenticationOptions> _configureAuthClaims = options =>
        {
            options.Resources = Constants.AllResourceNames;
            options.Subsets = MockRepositoryData.AllowedSubsetList.Select(s => s.Id).ToArray();
        };

        private readonly IoCConfig _iocConfig;
        private readonly Dictionary<string, string> _appSettingOverrides = new(StringComparer.OrdinalIgnoreCase){
            {"BrandVueApi.ApiResourceId", "local"},
            {"BrandVueApi.ApiResourceSecret", "local"},
            {"productsToLoadDataFor", "barometer"},
            {"PublicApi.DefaultGetMetricsResultLimit", "500"},
            {"AnswersConnectionString", @"Server=.\sql2022;Database=VueExport;TrustServerCertificate=True;Trusted_Connection=True;Integrated Security=True;MultipleActiveResultSets=true;Encrypt=True;"},
            {"MetaConnectionString", @"Server=.\sql2022;Database=BrandVueMeta;TrustServerCertificate=True;Trusted_Connection=True;Integrated Security=True;MultipleActiveResultSets=true;Encrypt=True;"},
            {"AzureADGroupCanAccessRespondentLevelDownload", "azure-ad-group-guid"},
        };

        private TestServer TestServer => _testServerBackingField ??= CreateTestServer();

        public ILifetimeScope Container =>
            ((Autofac.Extensions.DependencyInjection.AutofacServiceProvider)TestServer.Services).LifetimeScope;

        public BrandVueTestServer(ApiTestConfig testConfig = null, ProductContext productContext = null, IoCConfig iocConfig = null)
        {
            _apiTestConfig = testConfig ?? CreateDefaultApiTestConfig();
            _productContext = productContext;
            _iocConfig = iocConfig;
        }
        private static ApiTestConfig CreateDefaultApiTestConfig() => new ApiTestConfig($"/{DataController.Route}");

        private BrandVueApiTestIoCConfig CreateDefaultIocConfig(IConfiguration configuration)
        {
            var substituteLogFactory = Substitute.For<ILoggerFactory>();
            var mixpanelClient = Substitute.For<IMixpanelClient>();
            var appSettings = new AppSettings(appSettingsCollection: CreateCommonAppSettings(), configuration: configuration);

            // Extract roles from _configureAuthClaims
            var localAuthOptions = new LocalAuthenticationOptions();
            _configureAuthClaims(localAuthOptions);
            var roles = localAuthOptions.Role;

            var brandVueApiTestIoCConfig = new BrandVueApiTestIoCConfig(
                substituteLogFactory,
                appSettings,
                _productContext ?? new ProductContext(appSettings.ProductToLoadDataFor),
                Substitute.For<IOptions<MixPanelSettings>>(),
                Substitute.For<IOptions<ProductSettings>>(),
                configuration
            );

            Init(mixpanelClient, Substitute.For<ILogger<MixPanelLogger>>(), "barometer");
            return brandVueApiTestIoCConfig;
        }

        private TestServer CreateTestServer()
        {
            var builder = AppSettings.BuilderFromFile(Environment.CurrentDirectory).AddInMemoryCollection(_appSettingOverrides);
            var configuration = builder.Build();
            var brandVueApiTestOwinStartup = new Startup(configuration)
            {
                IoCConfig = _iocConfig ?? CreateDefaultIocConfig(configuration)
            };
            brandVueApiTestOwinStartup.ConfigureLocallyFabricatedClaims += _configureAuthClaims;

            Assert.That(RouteConfig.UseSubProductPathPrefix, Is.False, "These tests won't work while UseSubProductPathPrefix is set to true since it's read during static load. It should be overriden by appsettings.override.json");

            return new TestWebApplicationFactory(brandVueApiTestOwinStartup).Server;
        }

        public BrandVueTestServer WithAccessToResources(string[] resources) => NewWithAddedAction(options => options.Resources = resources);

        public BrandVueTestServer WithAccessToProducts(string[] products) => NewWithAddedAction(options => options.Products = products);

        public BrandVueTestServer WithRole(string roleName) => NewWithAddedAction(options => options.Role = roleName);

        public BrandVueTestServer WithTrialEndDate(DateTimeOffset trialEndDate) => NewWithAddedAction(options => options.TrialEndDate = JsonConvert.SerializeObject(trialEndDate));

        public BrandVueTestServer WithOnlyTheseClaimTypes(IReadOnlySet<string> claimTypes) => NewWithAddedAction(options => options.ClaimTypes = claimTypes);

        public BrandVueTestServer WithOnly(Action<HttpClient> configure) => new(_apiTestConfig.With(configure));

        public BrandVueTestServer WithoutFabricatedClaims() => new(_apiTestConfig){_configureAuthClaims = options => options.FabricateClaimsIfLocal = false };

        public async Task<string> PostAsyncAssert(string url, object requestContent, HttpStatusCode expectedStatusCode) =>
            await PostAsyncAssert<string>(url, requestContent, expectedStatusCode);

        public async Task<TResult> PostAsyncAssert<TResult>(string url, object requestContent, HttpStatusCode expectedStatusCode, Type overrideRequestContentSerializationType = null)  where TResult: class
        {
            var requestContentSerializationType = overrideRequestContentSerializationType ?? requestContent?.GetType() ?? typeof(object);
            using (var httpClient = TestServerHttpClient())
            using (var objectContent = JsonContent.Create(requestContent, requestContentSerializationType))
            using (var actualResponse = await httpClient.PostAsync(url, objectContent))
            {
                await AssertStatusCode(expectedStatusCode, actualResponse);
                return await _apiTestConfig.ReadAsync<TResult>(actualResponse);
            }
        }

        public async Task<IEnumerable<T>> GetAsyncAssertLengthOk<T>(string url, IEnumerable<T> expectedOkResponseContent)
        {
            var actualContent = (await GetAsyncAssert<IEnumerable<T>>(url, HttpStatusCode.OK)).ToList();
            Assert.That(expectedOkResponseContent.Count(), Is.EqualTo(actualContent.Count()));
            return actualContent;
        }
        
        public async Task<TContent> GetAsyncAssertOk<TContent>(string url, TContent expectedOkResponseContent) where TContent: class
        {
            var actualContent = await GetAsyncAssert<TContent>(url, HttpStatusCode.OK);
            Assert.That(actualContent, Is.EqualTo(expectedOkResponseContent));
            return actualContent;
        }
        public async Task<IList<TContent>> GetAsyncAssertOk<TContent>(string url, IEnumerable<TContent> expectedOkResponseContent, IEqualityComparer<TContent> comparer) where TContent : class
        {
            TestContext.AddFormatter<TContent>(c => JsonConvert.SerializeObject(c, Formatting.Indented));
            var actualContent = await GetAsyncAssert<IList<TContent>>(url, HttpStatusCode.OK);
            Assert.That(actualContent, Is.EqualTo(expectedOkResponseContent).Using(comparer));
            return actualContent;
        }

        public async Task<string> GetAsyncAssert(string url, HttpStatusCode expectedStatusCode) =>
            await GetAsyncAssert<string>(url, expectedStatusCode);

        public async Task<TContent> GetAsyncAssert<TContent>(string url, HttpStatusCode expectedStatusCode) where TContent: class
        {
            using (var httpClient = TestServerHttpClient())
            using (var actualResponse = await httpClient.GetAsync(url))
            {
                await AssertStatusCode(expectedStatusCode, actualResponse);
                return await _apiTestConfig.ReadAsync<TContent>(actualResponse);
            }
        }

        private BrandVueTestServer NewWithAddedAction(Action<LocalAuthenticationOptions> action) => new(_apiTestConfig)
        {
            _configureAuthClaims = _configureAuthClaims.Combine(action)
        };

        private static async Task AssertStatusCode(HttpStatusCode expectedStatusCode, HttpResponseMessage actualResponse)
        {
            var contentText = await actualResponse.Content.ReadAsStringAsync();
            Assert.That(actualResponse.StatusCode, Is.EqualTo(expectedStatusCode),
                () => "Response: " + contentText
            );
        }

        internal HttpClient TestServerHttpClient()
        {
            var testServerHttpClient = new HttpClient(TestServer.CreateHandler()) {BaseAddress = new Uri("http://testorg.testdomain")};
            _apiTestConfig.SetDefaultHttpClientOptions(TestServer, testServerHttpClient);
            return testServerHttpClient;
        }

        public BrandVueTestServer OverrideAppSettings(Dictionary<string, string> settings)
        {
            foreach(var setting in settings)
            {
                _appSettingOverrides[setting.Key] = setting.Value;
            }
            return this;
        }
    }
}
