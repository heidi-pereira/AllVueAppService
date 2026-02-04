using BrandVue.SourceData.CalculationPipeline;

namespace BrandVue.Models
{
    public static class OrderExtensions
    {
        public static EntityWeightedDailyResults[] OrderByFocusEntityInstanceAndThenAlphabeticByEntityInstanceName(
            this IEnumerable<EntityWeightedDailyResults> results, long ? focusEntityInstanceId)
        {
            if (focusEntityInstanceId.HasValue)
            {
                return results.Any(b => b.EntityInstance == null) ? results.ToArray() : results.OrderBy(x => x.EntityInstance.Id == focusEntityInstanceId ? -1 : 1).ThenBy(x => x.EntityInstance.Name).ToArray();
            }
            return results.Any(b => b.EntityInstance == null) ? results.ToArray() : results.OrderBy(x => x.EntityInstance.Name).ToArray();
        }
        public static BrokenDownResults[] OrderByFocusEntityInstanceAndThenAlphabeticByEntityInstanceName(
            this IEnumerable<BrokenDownResults> results, long? focusEntityInstanceId)
        {
            return results.Any(b => b.EntityInstance == null) ? results.ToArray() : results.OrderBy(x => x.EntityInstance.Id == focusEntityInstanceId ? -1 : 1).ThenBy(x => x.EntityInstance.Name).ToArray();
        }

        public static MultiMetricSeries[] OrderReverseAlphabeticByEntityInstanceName(this IEnumerable<MultiMetricSeries> results)
        {
            return results.Any(b => b.EntityInstance == null) ? results.ToArray() : results.OrderByDescending(x => x.EntityInstance.Name).ToArray();
        }
    }
}