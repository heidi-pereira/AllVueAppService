using BrandVue.SourceData.Calculation;
using Newtonsoft.Json;

namespace BrandVue.SourceData.CalculationPipeline
{
    public class Total
    {
        public IList<int> ResponseIdsForPeriod { get; set; }
        public IList<int> ResponseIdsForAverage { get; set; }
        public ResultSampleSizePair TotalForPeriodOnly { get; set; }
        public ResultSampleSizePair TotalForAverage { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }
    }
}
