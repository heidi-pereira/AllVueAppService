using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Measures;

namespace BrandVue.Models
{
    public class GroupedCrossbreakCompetitionResults
    {
        public IReadOnlyCollection<GroupedBreakResults> GroupedBreakResults { get; set; }
    }

    public class GroupedBreakResults
    {
        public string GroupName { get; set; }
        public CrossbreakCompetitionResults BreakResults { get; set; }
    }

    public class CrossbreakCompetitionResults : AbstractCommonResultsInformation
    {
        public IReadOnlyCollection<BreakResults> InstanceResults { get; set; }
    }

    public class BreakResults
    {
        public string BreakName { get; set; }
        public int? BreakEntityInstanceId { get; set; }
        public EntityWeightedDailyResults[] EntityResults { get; set; }
    }
}
