using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Entity;

namespace BrandVue.Models
{
    public class RankingTableResult
    {
        public EntityInstance EntityInstance { get; init; }
        public int CurrentRank { get; init; }
        public bool MultipleWithCurrentRank { get; init; }
        public int? PreviousRank { get; init; }
        public WeightedDailyResult CurrentWeightedDailyResult { get; set; }
        public WeightedDailyResult PreviousWeightedDailyResult { get; set; }

        public RankingTableResult(EntityInstance entityInstance, int currentRank, int? previousRank, WeightedDailyResult currentWeightedDailyResult, WeightedDailyResult previousWeightedDailyResult, bool multipleWithCurrentRank)
        {
            EntityInstance = entityInstance;
            CurrentRank = currentRank;
            PreviousRank = previousRank;
            CurrentWeightedDailyResult = currentWeightedDailyResult;
            PreviousWeightedDailyResult = previousWeightedDailyResult;
            MultipleWithCurrentRank = multipleWithCurrentRank;
        }
    }
}