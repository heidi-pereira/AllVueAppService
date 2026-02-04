namespace BrandVue.SourceData.CommonMetadata
{
    public class CommonMetadataFieldApplicator : ICommonMetadataFieldApplicator
    {
        private readonly AppSettings _appSettings;

        public CommonMetadataFieldApplicator(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        public static void ApplyDisabled(
            IDisableable disableable,
            string [] headers,
            string [] currentRecord)
        {
            //  Just in case people don't apply fields in the "right" order:
            //  we don't want to unset this if it's been set elsewhere.
            if (!disableable.Disabled)
            {
                disableable.Disabled
                    = FieldExtractor.ExtractBoolean(
                        CommonMetadataFields.Disabled,
                        headers,
                        currentRecord,
                        false);
            }
        }

        public static void ApplySubsets(
            ISubsetConfigurable metadataObject,
            ISubsetRepository repository,
            string [] headers,
            string [] currentRecord)
        {
            var rawSubsets = FieldExtractor.ExtractStringArray(
                CommonMetadataFields.Subset,
                headers,
                currentRecord,
                true);

            if (rawSubsets != null)
            {
                metadataObject.SetSubsets(rawSubsets, repository);
            }
        }

        public static void ApplySubsets(
            ISubsetConfigurable metadataObject,
            ISubsetRepository repository, 
            string rawValue)
        {
            var rawSubsets = FieldExtractor.ExtractStringArray(rawValue);

            if (rawSubsets != null)
            {
                metadataObject.SetSubsets(rawSubsets, repository);
            }
        }

        public void ApplyEnvironment<T>(
            T disableable,
            string[] headers,
            string[] currentRecord) where T: IEnvironmentConfigurable, IDisableable
        {
            disableable.Environment = FieldExtractor.ExtractStringArray(
                CommonMetadataFields.Environment,
                headers,
                currentRecord,
                true);

            if (!_appSettings.IsDeployedEnvironmentOneOfThese(
                disableable.Environment))
            {
                disableable.Disabled = true;
            }
        }
        public void ApplyAvailability<T>(
            T disableable,
            ISubsetRepository subsetRepository,
            string [] headers,
            string [] currentRecord) where T: IEnvironmentConfigurable, IDisableable, ISubsetConfigurable
        {
            ApplyDisabled(
                disableable, headers, currentRecord);
            ApplySubsets(
                disableable, subsetRepository, headers, currentRecord);
            ApplyEnvironment(
                disableable, headers, currentRecord);
        }
    }
}
