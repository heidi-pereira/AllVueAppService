namespace BrandVue.Models
{
    public class RankingOvertimeResults : AbstractCommonResultsInformation
    {
        public IList<RankingOvertimeResultsByDate> Results { get; }

        public RankingOvertimeResults(IList<RankingOvertimeResultsByDate> results)
        {
            Results = results;
        }
    }
}
