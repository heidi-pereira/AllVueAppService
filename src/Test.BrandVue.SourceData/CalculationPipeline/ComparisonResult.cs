using System.Collections.Generic;

namespace Test.BrandVue.SourceData.CalculationPipeline
{
    public class ComparisonResult
    {
        public string TestDataName { get; set; }
        public bool IsMatch { get; set; }
        public int SnowflakeCount { get; set; }
        public int SqlServerCount { get; set; }
        public List<string> Differences { get; set; } = new();
    }
}
