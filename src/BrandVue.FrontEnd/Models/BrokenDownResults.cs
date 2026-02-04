using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;

namespace BrandVue.Models
{
    public class BrokenDownResults
    {
        public EntityInstance EntityInstance { get; }
        public IList<CategoryResults> ByAgeGroup { get; }
        public IList<WeightedDailyResult> Total { get; }
        public Measure Measure { get; }
        public IList<CategoryResults> ByGender { get; }
        public IList<CategoryResults> ByRegion { get; }
        public IList<CategoryResults> BySocioEconomicGroup { get; }

        public BrokenDownResults(Measure measure, EntityInstance entityInstance,
            IList<CategoryResults> byAgeGroup,
            IList<CategoryResults> byGender,
            IList<CategoryResults> byRegion,
            IList<CategoryResults> bySocioEconomicGroup,
            IList<WeightedDailyResult> total)
        {
            EntityInstance = entityInstance;
            ByAgeGroup = byAgeGroup;
            Measure = measure;
            ByGender = byGender;
            ByRegion = byRegion;
            BySocioEconomicGroup = bySocioEconomicGroup;
            Total = total;
        }
    }
}