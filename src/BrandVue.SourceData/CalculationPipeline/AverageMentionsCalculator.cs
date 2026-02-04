using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline
{
    internal class AverageMentionsCalculator
    {
        private readonly IQuotaCellReferenceWeightingRepository _quotaCellReferenceWeightingRepository;
        private readonly IProfileResponseAccessor _profileResponseAccessor;

        public AverageMentionsCalculator(IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository, IProfileResponseAccessor profileResponseAccessor)
        {
            _quotaCellReferenceWeightingRepository = quotaCellReferenceWeightingRepository;
            _profileResponseAccessor = profileResponseAccessor;
        }

        public WeightedDailyResult GetAverageMentions(Subset subset,
            AverageDescriptor averageWithIncludeResponseIdsOverride,
            Measure measure, IGroupedQuotaCells quotaCells,
            EntityWeightedDailyResults[] weighted, IEnumerable<CellTotals> dailyResultsWithResponseIds)
        {
            var responseIdsWithWeights = dailyResultsWithResponseIds.SelectMany(dailyResult => WeightGeneratorForRequestedPeriod.ResponseWeightsForDay(_quotaCellReferenceWeightingRepository, subset, averageWithIncludeResponseIdsOverride, quotaCells, _profileResponseAccessor, dailyResult)
            ).GroupBy(r => r.ResponseId).ToArray();

            return GetAverageMentions(measure, responseIdsWithWeights, weighted);
        }

        public static WeightedDailyResult GetAverageMentions(Measure measure, IGrouping<int, ResponseWeight>[] responseIdsWithWeights, EntityWeightedDailyResults[] weighted)
        {
            var weightedAnswerCount = measure.CalculationType == CalculationType.Average ?
                weighted.Sum(r => r.WeightedDailyResults.SingleOrDefault()?.WeightedSampleSize ?? 0.0) :
                weighted.Sum(r => r.WeightedDailyResults.SingleOrDefault()?.WeightedValueTotal ?? 0.0);
            var weightedSampleSize = responseIdsWithWeights.Sum(g => g.First().Weight);
            var unweightedSampleSize = responseIdsWithWeights.Count();
            var averageMentionsWeighted = weightedSampleSize > 0.0 ? weightedAnswerCount / weightedSampleSize : 0.0;

            return new WeightedDailyResult(weighted.FirstOrDefault()?.WeightedDailyResults.SingleOrDefault()?.Date ?? DateTimeOffset.Now)
            {
                WeightedResult = averageMentionsWeighted,
                UnweightedSampleSize = (uint)unweightedSampleSize,
                WeightedSampleSize = weightedSampleSize,
                Text = AverageHelper.GetAverageDisplayText(AverageType.Mentions)
            };
        }
    }
}