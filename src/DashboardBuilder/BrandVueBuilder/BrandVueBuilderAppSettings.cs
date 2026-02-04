namespace BrandVueBuilder
{
    public class BrandViewMetaBuilderAppSettings
    {
        public string OutputPathMetadata { get; }
        public string OutputPathConfig { get; }
        public string SourceFolder { get; }
        public string BaseFolder { get; }
        public bool RequiresV2CompatibleFieldModel { get; }

        public BrandViewMetaBuilderAppSettings(string outputPathConfig, string outputPathMetadata, string sourceFolder,
            string baseFolder, bool requiresV2CompatibleFieldModel)
        {
            OutputPathConfig = outputPathConfig;
            OutputPathMetadata = outputPathMetadata;
            SourceFolder = sourceFolder;
            BaseFolder = baseFolder;
            RequiresV2CompatibleFieldModel = requiresV2CompatibleFieldModel;
        }
    }
    public class BrandVueBuilderAppSettings
    {
        public BrandViewMetaBuilderAppSettings MetaAppSettings { get; }

        public BrandVueBuilderAppSettings(BrandViewMetaBuilderAppSettings metaBuilderAppSettings)
        {
            MetaAppSettings = metaBuilderAppSettings;
        }
    }
}