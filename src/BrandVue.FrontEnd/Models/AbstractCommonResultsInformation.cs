namespace BrandVue.Models
{
    public abstract class AbstractCommonResultsInformation : ICommonResultsInformation, IDataResultsInformation
    {
        public virtual string TypeName => GetType().Name;
        public SampleSizeMetadata SampleSizeMetadata { get; set; }
        public bool HasData { get; set; } = true;
        public LowSampleSummary[] LowSampleSummary { get; set; }
        public bool TrialRestrictedData { get; set; }
    }
}