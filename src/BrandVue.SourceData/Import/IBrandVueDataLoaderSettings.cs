using BrandVue.SourceData.CommonMetadata;

namespace BrandVue.SourceData.Import
{
    public interface IBrandVueDataLoaderSettings
    {
        string BaseMetadataPath { get; }
        string EntityFieldsMetadataFilepath(EntityType entityType);
        string SubsetMetadataFilepath { get; }
        string AverageMetadataFilePath { get; }
        string PageMetadataFilepath { get; }
        string PaneMetadataFilepath { get; }
        string PartMetadataFilepath { get; }
        string MeasureMetadataFilepath { get; }
        string SettingsMetadataFilepath { get; }
        string FilterMetadataFilepath { get; }
        string ProfilingFieldsMetadataFilepath { get; }
        string ResponseEntityTypesMetadataFilepath { get; }
        string FieldDefinitionsDataFilepath { get; }
        string ConnectionString { get; }
        string SurveysMetadataFilePath { get; }
        bool LoadConfigFromSql { get; }
        bool FeatureFlagBrandVueLoadWeightingFromDatabase { get; }
        string WeightingsFilepath(Subset subset);
        string RespondentProfileDataFilepath(Subset subset);
        string BrandResponseDataFilepath(Subset subset);
        AppSettings AppSettings { get; }
        bool AutoCreateEntities { get; }
        int MaxConcurrentDataLoaders { get; }
    }
}