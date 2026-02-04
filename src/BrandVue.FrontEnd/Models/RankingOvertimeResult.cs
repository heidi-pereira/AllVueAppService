using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Entity;

namespace BrandVue.Models
{
    public class RankingOvertimeResult
    {
        public EntityInstance EntityInstance { get; }
        public int Rank { get; }
        public WeightedDailyResult WeightedDailyResult { get; set; }
        public bool MultipleSameRank { get; }

        public RankingOvertimeResult(EntityInstance entityInstance, int rank, WeightedDailyResult currentWeightedDailyResult, bool multipleSameRank)
        {
            EntityInstance = entityInstance;
            Rank = rank;
            WeightedDailyResult = currentWeightedDailyResult;
            MultipleSameRank = multipleSameRank;
        }
    }
}
