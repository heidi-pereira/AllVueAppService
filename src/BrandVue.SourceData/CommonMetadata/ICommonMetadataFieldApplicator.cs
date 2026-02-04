namespace BrandVue.SourceData.CommonMetadata
{
    public interface ICommonMetadataFieldApplicator
    {
        void ApplyEnvironment<T>(
            T disableable,
            string[] headers,
            string[] currentRecord) where T: IEnvironmentConfigurable, IDisableable;

        void ApplyAvailability<T>(
            T disableable,
            ISubsetRepository subsetRepository,
            string [] headers,
            string [] currentRecord) where T: IEnvironmentConfigurable, IDisableable, ISubsetConfigurable;
    }
}