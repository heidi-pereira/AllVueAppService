namespace BrandVue.SourceData.QuotaCells;

public static class PopulatedQuotaCellsExtensions
{
    public static IEnumerable<PopulatedQuotaCell> WithinTimesInclusive(this IEnumerable<PopulatedQuotaCell> cells, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var beforeStart =
            startDate.ToDateInstance().Ticks - 1; // Use timestamp not in array to avoid issues with duplicates
        var afterEnd = endDate.ToDateInstance().Ticks + 1; // Use timestamp not in array to avoid issues with duplicates
        return cells
            .Select(p => p.WithinTimesInclusive(beforeStart, afterEnd))
            .Where(p => p.Profiles.Length > 0);
    }
}