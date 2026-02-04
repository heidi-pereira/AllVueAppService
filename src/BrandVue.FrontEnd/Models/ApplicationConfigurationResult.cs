namespace BrandVue.Models
{
    [Flags]
    //https://stackoverflow.com/questions/37936552/flags-and-operation-on-enums-c-sharp
    public enum Features
    {
        None                                = 0x00000,    //Caution these names MUST NOT contain substrings of others
        ChartConfiguration                  = 1 << 0,
        FeatureFlagAllowReadingOfWeightsViaWeightingPlan = 1 << 6,
        FeatureFlagNewWeightingUIAvailable = 1 << 12,
        FeatureFlagAilaTextSummarisation = 1 << 14
    }
    
    [Flags]
    public enum AdditionalProductFeature
    {
        None=0,
        DataTabAvailable=2,
        ReportTabAvailable=4,
        QuotaTabAvailable=8,
        DocumentsTabAvailable = 16,
        HelpIconAvailable=32
    }
    public enum RunningEnvironment
    {
        Development,
        Live,
        Unknown,
    }
    public class ApplicationConfigurationResult
    {
        public DateTimeOffset DateOfFirstDataPoint { get; set; }
        public DateTimeOffset DateOfLastDataPoint { get; set; }
        public bool HasLoadedData { get; set; }
    }
}
