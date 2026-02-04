using System.Collections.Generic;

namespace Test.BrandVue.SourceData.CalculationPipeline
{
    /// <summary>
    /// Model for deserializing test data from JSON files
    /// </summary>
    public class TestDataModel
    {
        public string TestDataName { get; set; }
        public string VarCodeBase { get; set; }
        public List<ResponseWeightJson> ResponseWeights { get; set; }
        public List<FilterJson> Filters { get; set; }
    }

    public class ResponseWeightJson
    {
        public int ResponseId { get; set; }
        public double Weight { get; set; }
    }

    public class FilterJson
    {
        public string Location { get; set; }
        public int Id { get; set; }
    }
}
