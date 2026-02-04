using BrandVue.EntityFramework;
using BrandVue.Models;
using BrandVue.SourceData.Subsets;
using Microsoft.Extensions.Configuration;

namespace BrandVue.Services
{
    public class InitialWebAppConfig
    {
        private const string MIXPANEL = "MixPanel";
        private readonly AppSettings _appSettings;
        private readonly IConfiguration _configuration;
        public InitialWebAppConfig(AppSettings appSettings, IConfiguration configuration)
        {
            _appSettings = appSettings;
            _configuration = configuration;
        }
        private bool IsDev => RunningEnvironmentDescription == AppSettings.DevEnvironmentName;
        private bool IsLive => RunningEnvironmentDescription == AppSettings.OctopusLiveEnvironmentName;
        public string RunningEnvironmentDescription => _appSettings.GetGlobalSetting<string>("environment");
        public RunningEnvironment RunningEnvironment => IsDev? RunningEnvironment.Development : IsLive ? RunningEnvironment.Live: RunningEnvironment.Unknown;
        public string cdnAssetsEndpoint => _appSettings.GetGlobalSetting<string>("cdnAssetsEndpoint");
        public bool LoadConfigFromSql => _appSettings.GetGlobalSetting<bool>(nameof(LoadConfigFromSql));
        public bool FeatureFlagBrandVueLoadWeightingFromDatabase => _appSettings.GetGlobalSetting<bool>(nameof(FeatureFlagBrandVueLoadWeightingFromDatabase));
        public bool FeatureFlagNewWeightingUIAvailable => _appSettings.GetGlobalSetting<bool>(nameof(FeatureFlagNewWeightingUIAvailable));
        public bool FeatureFlagAilaTextSummarisation => _appSettings.GetGlobalSetting<bool>(nameof(FeatureFlagAilaTextSummarisation));
        public string DefaultActiveBrandForGeography(Subset subset)
            => _appSettings.GetSubSetting(nameof(DefaultActiveBrandForGeography), subset.Id).Single();
        public string DefaultActivePeerGroupForGeography(Subset subset)
            => _appSettings.GetSubSetting(nameof(DefaultActivePeerGroupForGeography), subset.Id).Single();

        [Obsolete("Use GaTags, but remember to prefix any existing configured ones with GTM- which was automatically added here previously")]
        public string[] GoogleTags => ParseCommaSeparatedString(_appSettings.GetSetting(nameof(GoogleTags)));
        public string[] GaTags => ParseCommaSeparatedString(_appSettings.GetSetting(nameof(GaTags)));
        public ushort LowSampleForBrand => _appSettings.GetGlobalSetting<ushort>(nameof(LowSampleForBrand));
        public int NoSampleForBrand => _appSettings.GetGlobalSetting<int>(nameof(NoSampleForBrand));
        public int DefaultGetMetricsResultLimit => _appSettings.GetGlobalSetting<int>("PublicApi.DefaultGetMetricsResultLimit");
        public string ProductsToLoadDataFor => _appSettings.GetGlobalSetting<string>(nameof(ProductsToLoadDataFor));
        public bool ChartConfigurationEnabled => _appSettings.GetGlobalSetting<bool>(nameof(ChartConfigurationEnabled));
        public bool BrowserCachingEnabled => _appSettings.GetGlobalSetting<bool>(nameof(BrowserCachingEnabled));
        public bool ShouldGenerateAppendix => _appSettings.GetGlobalSetting<bool>(nameof(ShouldGenerateAppendix));
        public string CustomerPortalQuotaLink => _appSettings.GetGlobalSetting<string>(nameof(CustomerPortalQuotaLink));
        public string CustomerPortalDocumentLink => _appSettings.GetGlobalSetting<string>(nameof(CustomerPortalDocumentLink));
        public string CustomerPortalStatusLink => _appSettings.GetGlobalSetting<string>(nameof(CustomerPortalStatusLink));
        public string SurveyManagementLink => _appSettings.GetGlobalSetting<string>(nameof(SurveyManagementLink));
        public string OpenEndsLink => _appSettings.GetGlobalSetting<string>(nameof(OpenEndsLink));
        public string AuthServerUrl => _appSettings.GetGlobalSetting<string>(nameof(AuthServerUrl));
        public string BrandVueHelpLink => _appSettings.GetGlobalSetting<string>(nameof(BrandVueHelpLink));
        public string AllVueHelpLink => _appSettings.GetGlobalSetting<string>(nameof(AllVueHelpLink));
        public string BrandVueToken => _configuration["MixPanel:BrandVueToken"];
        public string AllVueToken => _configuration["MixPanel:AllVueToken"];
        private static string[] ParseCommaSeparatedString(string commaSeparatedWithIrrelevantSurroundingWhitespace)
        {
            return commaSeparatedWithIrrelevantSurroundingWhitespace
                .Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();
        }
    }
}
