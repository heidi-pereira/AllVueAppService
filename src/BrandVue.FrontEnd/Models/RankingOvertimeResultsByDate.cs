namespace BrandVue.Models
{
    public class RankingOvertimeResultsByDate
    {
        public DateTimeOffset Date { get; }
        public IList<RankingOvertimeResult> Results { get; }

        public RankingOvertimeResultsByDate(DateTimeOffset date, IList<RankingOvertimeResult> results)
        {
            Date = date;
            Results = results;
        }
    }
}
