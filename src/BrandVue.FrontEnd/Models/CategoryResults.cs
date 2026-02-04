using BrandVue.SourceData.Calculation;

namespace BrandVue.Models
{
    public class CategoryResults
    {
        public string Category { get; }
        public IList<WeightedDailyResult> WeightedDailyResults { get; }

        public CategoryResults(string category, IList<WeightedDailyResult> weightedDailyResults)
        {
            Category = category;
            WeightedDailyResults = weightedDailyResults;
        }
    }
}