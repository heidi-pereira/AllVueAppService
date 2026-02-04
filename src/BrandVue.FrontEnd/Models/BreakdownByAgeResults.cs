using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Entity;

namespace BrandVue.Models
{
    public class BreakdownByAgeResults : AbstractCommonResultsInformation
    {
        public EntityInstance EntityInstance { get; set; }
        public ICollection<CategoryResults> ByAgeGroup { get; set; }
        public WeightedDailyResult[] Total { get; set; }
    }
}