using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Measures;

namespace BrandVue.Models
{
    public class ResultsForMeasure
    {
        public Measure Measure { get; set; }
        public EntityWeightedDailyResults[] Data { get; set; }
        public string NumberFormat { get; set; }
    }
}