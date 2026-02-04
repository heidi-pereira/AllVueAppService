using System.IO;
using BrandVue.SourceData.CommonMetadata;

namespace BrandVue.SourceData.Import
{
    public class ConfigurationSourcedLoaderSettings : IBrandVueDataLoaderSettings
    {
        public AppSettings AppSettings { get; }
        private readonly MetadataPaths _metadataPaths;
        public string ProductName { get; }

        public ConfigurationSourcedLoaderSettings(AppSettings appSettings)
        {
            AppSettings = appSettings;
            ProductName = AppSettings.ProductToLoadDataFor;
            _metadataPaths = new MetadataPaths(AppSettings);
        }

        public string BaseMetadataPath => AppSettings.GetRootedPathWithProductNameReplaced("baseMetadataPath");
        public string BaseWeightingsPath => AppSettings.GetRootedPathWithProductNameReplaced("baseWeightingsPath");
        private string BaseDataPath => AppSettings.GetRootedPathWithProductNameReplaced("baseDataPath");
        public string FilterMetadataFilepath => _metadataPaths.Filters;
        public string ProfilingFieldsMetadataFilepath => _metadataPaths.ProfilingFields;
        public string SubsetMetadataFilepath => _metadataPaths.Subsets;
        public string SurveysMetadataFilePath => _metadataPaths.Surveys;
        public string AverageMetadataFilePath => _metadataPaths.Averages;
        public string PageMetadataFilepath => _metadataPaths.DashPages;
        public string PaneMetadataFilepath => _metadataPaths.DashPanes;
        public string PartMetadataFilepath => _metadataPaths.DashParts;
        public string MeasureMetadataFilepath => _metadataPaths.Measures;
        public string SettingsMetadataFilepath => _metadataPaths.Settings;
        public string ResponseEntityTypesMetadataFilepath => _metadataPaths.ResponseEntityTypes;
        public string FieldDefinitionsDataFilepath => _metadataPaths.FieldDefinitions;
        public string ConnectionString => AppSettings.ConnectionString?.Replace("{ProductName}", ProductName);
        public int MaxConcurrentDataLoaders => AppSettings.MaxConcurrentDataLoaders;
        public string EntityFieldsMetadataFilepath(EntityType entityType)
        {
            string fileName;
            if (entityType.IsProfile)
            {
                fileName = "profilingFields.csv";
            }
            else if (entityType.IsBrand)
            {
                fileName = "brandFields.csv";
            }
            else
            {
                throw new ArgumentOutOfRangeException(
                    nameof(entityType),
                    entityType,
                    $@"Unable to get entity fields metadata filepath for entity of type '{
                            entityType
                        }' under '{
                            _metadataPaths.Base
                        }' since this type of entity is not supported.");
            }

            return Path.Combine(_metadataPaths.Base, fileName);
        }

        public string WeightingsFilepath(Subset subset)
        {
            string subsetSpecificOverridePath = GetWeightingsPathFor(subset.Id);
            string countryWeightingsFilepath = GetWeightingsPathFor(subset.Iso2LetterCountryCode);
            return !File.Exists(subsetSpecificOverridePath) && File.Exists(countryWeightingsFilepath) ? countryWeightingsFilepath : subsetSpecificOverridePath;
        }

        private string GetWeightingsPathFor(string specific)
        {
            return Path.Combine(
                BaseWeightingsPath,
                AppSettings.GetSetting("weightingsFilename")).Replace(
                "{Geography}",
                specific);
        }

        public string RespondentProfileDataFilepath(Subset subset)
            => Path.Combine(
                BaseDataPath,
                AppSettings.GetSetting("respondentProfileDataFilename")).Replace(
                    "{Geography}",
                    subset.Id);

        public string BrandResponseDataFilepath(Subset subset)
            => Path.Combine(
                BaseDataPath,
                AppSettings.GetSetting("brandResponseDataFilename")).Replace(
                    "{Geography}",
                    subset.Id);

        public bool LoadConfigFromSql => "true".Equals(AppSettings.GetSetting("loadConfigFromSql"), StringComparison.InvariantCultureIgnoreCase);
        public bool FeatureFlagBrandVueLoadWeightingFromDatabase => "true".Equals(AppSettings.GetSetting("FeatureFlagBrandVueLoadWeightingFromDatabase"), StringComparison.InvariantCultureIgnoreCase);
        public bool AutoCreateEntities => "true".Equals(AppSettings.GetSetting("AutoCreateEntities"), StringComparison.InvariantCultureIgnoreCase);
    }
}
