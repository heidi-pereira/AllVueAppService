using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline
{
    internal static class WeightingAggregatorFactory
    {
        public static IWeightingAggregator Create(IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository, IProfileResponseAccessor profileResponseAccessor, AverageDescriptor averageDescriptor)
        {
            var allPeriodsWeightingAggregator = new AllPeriodsWeightingAggregator(quotaCellReferenceWeightingRepository, profileResponseAccessor);
            return averageDescriptor.MakeUpTo switch
            {
                MakeUpTo.WeekEnd => new PreWeightingAggregatorDecorator(allPeriodsWeightingAggregator, result => result.Date.DayOfWeek == DayOfWeek.Sunday),
                _ => allPeriodsWeightingAggregator
            };
        }
    }
}