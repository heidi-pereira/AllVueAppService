namespace BrandVue.Models
{
    public class LowSampleSummary
    {
        public long? EntityInstanceId { get; set; }
        public string Name { get; set; }
        public string Metric { get; set; }
        public DateTimeOffset? DateTime { get; set; }
    }
    public interface IDataResultsInformation
    {
        bool HasData { get; set; }
    }
    public interface ICommonResultsInformation
    {
        string TypeName { get; }
        SampleSizeMetadata SampleSizeMetadata { get; set; }
        LowSampleSummary[] LowSampleSummary { get; set; }
        bool TrialRestrictedData { get; set; }
    }
}