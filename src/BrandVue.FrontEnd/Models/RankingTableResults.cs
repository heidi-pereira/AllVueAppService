namespace BrandVue.Models
{
    public class RankingTableResults: AbstractCommonResultsInformation
    {
        public IList<RankingTableResult> Results { get; }

        public RankingTableResults(IList<RankingTableResult> results)
        {
            Results = results;
        }

    }
}