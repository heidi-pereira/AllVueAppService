using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace BrandVue.EntityFramework
{
    public record ApiSlidderSettings(
        bool Enabled,
        bool AutoReplenishment, 
        int PermitLimit,
        int QueueLimit,
        int WindowInSeconds,
        int QueueProcessingOrder,
        int SegmentsPerWindow)
    {
        [UsedImplicitly]
        public ApiSlidderSettings() : this(false, false,0,0,0,0,0)
        {

        }
    }

    public record ApiRateLimiting( ApiSlidderSettings PublicApi)
    {
        [UsedImplicitly]
        public ApiRateLimiting() : this(new ApiSlidderSettings())
        {
        }

    }
    
    public record SnowflakeDapperConfig
    {
        public string DatabaseName { get; init; }
        public string SchemaName { get; init; }
    }

    public class AppSettings : IMetaDataFactoryConfiguration
    {
        private readonly string _rootPath;
        public const string DevEnvironmentName = "dev";
        public const string LiveEnvironmentName = "live";
        public const string OctopusLiveEnvironmentName = "Live";
        public NameValueCollection AppSettingsCollection { get; }
        public string AllVueUploadFolder => GetSetting(nameof(AllVueUploadFolder));
        public string ConnectionString { get; }
        public bool FeatureFlagBrandVueLoadWeightingFromDatabase { get; }
        public string MetaConnectionString { get; }
        public string ProductToLoadDataFor { get; }
        public string AppDeploymentEnvironment { get; }
        public string ReportingApiAccessToken { get; }

        public IReadOnlyDictionary<string, SubProductSettings> ProductSettings => _configurationRoot
            .GetSection("ProductSettings").Get<IReadOnlyDictionary<string, SubProductSettings>>();

        public bool IsAppOnDeploymentBranch => !string.IsNullOrWhiteSpace(GetSetting("appDeploymentBranch"));
        public bool AllowLocalToBypassConfiguredAuthServer => GetSettingOrDefault(nameof(AllowLocalToBypassConfiguredAuthServer), false);
        public bool UseOptimisedCrossbreakCalculations => GetSettingOrDefault(nameof(UseOptimisedCrossbreakCalculations), false);

        private bool? _useDatabaseAssistedCalculationsForAudiences;
        public bool UseDatabaseAssistedCalculationsForAudiences
        {
            get => _useDatabaseAssistedCalculationsForAudiences ?? GetSettingOrDefault("useDatabaseAssistedCalculationsForAudiences", false);
            set => _useDatabaseAssistedCalculationsForAudiences = value;
        }

        public Dictionary<string, string[]> ExtraGaTagsByThirdPartyOrganisation { get; } = new(StringComparer.OrdinalIgnoreCase);

        public bool AllowEagerlyLoadingSubProducts => GetSettingOrDefault(nameof(AllowEagerlyLoadingSubProducts), false);
        public bool MigrateFields => GetSettingOrDefault(nameof(MigrateFields), false);
        public int MaxCartesianProductSize => GetSettingOrDefault(nameof(MaxCartesianProductSize), 500_000);
        public int MaxConcurrentDataLoaders { get; }

        public bool IsDeployedEnvironmentOneOfThese(params string[] environments)
        {
            var current = AppDeploymentEnvironment;
            return environments == null
                   || environments.Length == 0
                   || Array.Find(
                       environments,
                       environment => string.Equals(
                           environment,
                           current,
                           StringComparison.OrdinalIgnoreCase)) != null;
        }


        private static bool IsNpmScript { get; } = Environment.GetEnvironmentVariable("npm_command") is not null;
        private static bool IsTestRunner { get; } = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") is null && Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") is not null;

        /// <summary>
        /// In general, you don't need to reference this. It ensures we avoid overriding defaults with secrets.json.
        /// </summary>
        public static bool IsCompileTime { get; } = IsNpmScript || IsTestRunner;

        public AppSettings(string rootPath = null, NameValueCollection appSettingsCollection = null, string overrideAppSettingsJson = "appsettings.override.json", IConfiguration configuration = null)
        {
            _rootPath = rootPath ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            AppSettingsCollection = appSettingsCollection ?? ReadFromAppSettingsJson(_rootPath, overrideAppSettingsJson);
            ProductToLoadDataFor = GetGlobalSetting<string>("ProductsToLoadDataFor");
            MetaConnectionString = GetGlobalSetting<string>("MetaConnectionString");
            ConnectionString = GetGlobalSetting<string>("AnswersConnectionString");
            FeatureFlagBrandVueLoadWeightingFromDatabase = GetSettingOrDefault("FeatureFlagBrandVueLoadWeightingFromDatabase", true);
            AppDeploymentEnvironment = GetGlobalSetting<string>(nameof(AppDeploymentEnvironment));
            ReportingApiAccessToken = GetGlobalSetting<string>(nameof(ReportingApiAccessToken));
            MaxConcurrentDataLoaders = GetGlobalSetting<int>(nameof(MaxConcurrentDataLoaders));

            configuration?.GetSection("GaTags." + nameof(ExtraGaTagsByThirdPartyOrganisation)).Bind(ExtraGaTagsByThirdPartyOrganisation);
        }

        private static IConfigurationRoot _configurationRoot;

        public SnowflakeDapperConfig SnowflakeDapperConfig => _configurationRoot.GetSection("SnowflakeDapperDbSettings").Get<SnowflakeDapperConfig>();

        public string SnowflakeConnectionString => _configurationRoot["SnowflakeConnectionString"];

        public static NameValueCollection ReadFromAppSettingsJson(string rootPath, string overrideAppSettingsJson = "appsettings.override.json")
        {
            var builder = BuilderFromFile(rootPath, overrideAppSettingsJson);
            string aspNetCoreEnv = AspNetCoreEnv;
            if (aspNetCoreEnv is null or "Development" && !IsCompileTime)
            {
                builder.AddUserSecrets<AppSettings>(true);
            }

            var section = builder.Build();
            _configurationRoot = section;
            var collection = new NameValueCollection();
            section.GetChildren().ToList().ForEach(c => collection[c.Key] = c.Value);
            collection[nameof(AppDeploymentEnvironment)] ??=
                aspNetCoreEnv?.ToLower() switch
                {
                    "staging" => "beta",
                    "development" => DevEnvironmentName,
                    null => DevEnvironmentName,
                    var env => env
                };
            return collection;
        }

        public static IConfigurationBuilder BuilderFromFile(string rootPath, string overrideAppSettingsJson = "appsettings.override.json")
        {
            string aspNetCoreEnv = AspNetCoreEnv;
            return new ConfigurationBuilder()
                .SetBasePath(rootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{aspNetCoreEnv}.json", optional: true, reloadOnChange: true)
                .AddJsonFile(overrideAppSettingsJson, optional: true, reloadOnChange: true);
        }

        private static string AspNetCoreEnv => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        public string GetSetting(string propertyName) => AppSettingsCollection[propertyName];

        public string GetRootedPathWithProductNameReplaced(string propertyName)
        {
            return GetRootedPathSetting(propertyName).Replace("{ProductName}", ProductToLoadDataFor);
        }

        private string GetRootedPathSetting(string propertyName)
        {
            var setting = GetSetting(propertyName);
            return Path.IsPathRooted(setting) ? setting : Path.Combine(_rootPath, setting);
        }

        public string[] GetSubSetting(string settingName, string key)
        {
            string lookupString = GetSetting(settingName);
            var keyLookup = lookupString
                .Split('|')
                .Select(keyValuesString => keyValuesString.Split(':'))
                .ToDictionary(keyValuesPair => keyValuesPair[0].Trim(),
                    keyValuesPair => keyValuesPair[1].Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray());
            return keyLookup.TryGetValue(key, out var values) ? values : Array.Empty<string>();
        }

        public T GetGlobalSetting<T>(string key) => TryGetSetting<T>(key, out var value) ? value : throw new Exception("Can't find setting " + key);
        public T GetSettingOrDefault<T>(string key, T defaultSettingValue) => TryGetSetting<T>(key, out var v) ? v : defaultSettingValue;

        private bool TryGetSetting<T>(string key, out T settingValue)
        {
            var appSetting = AppSettingsCollection[key];
            if (string.IsNullOrWhiteSpace(appSetting))
            {
                settingValue = default;
                return false;
            }

            settingValue = FromString<T>(appSetting);
            return true;
        }

        private static T FromString<T>(string appSetting)
        {
            var converter = TypeDescriptor.GetConverter(typeof(T));
            return (T)converter.ConvertFromInvariantString(appSetting);
        }
    }
}
