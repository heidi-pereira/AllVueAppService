using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline
{
    internal class AllPeriodsWeightingAggregator : IWeightingAggregator
    {
        private readonly IQuotaCellReferenceWeightingRepository _quotaCellReferenceWeightingRepository;
        private readonly IProfileResponseAccessor _profileResponseAccessor;

        public AllPeriodsWeightingAggregator(IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository, IProfileResponseAccessor profileResponseAccessor)
        {
            _quotaCellReferenceWeightingRepository = quotaCellReferenceWeightingRepository;
            _profileResponseAccessor = profileResponseAccessor;
        }

        public EntityWeightedTotalSeries[] Weight(Subset datasetSelector,
            AverageDescriptor average,
            IGroupedQuotaCells indexOrderedDesiredQuotaCells,
            EntityTotalsSeries[] unweightedResults)
        {
            var instanceCount = unweightedResults.Length;
            var results = new EntityWeightedTotalSeries[instanceCount];
            if (instanceCount == 0)
            {
                return results;
            }
            int?[] numberOfResponsesInSelectedQuotaCellsByIndex = null;
            var weightsByResultIndex = new IDictionary<QuotaCell, double>[unweightedResults.Max(x => x.CellsTotalsSeries.Count)];
            for (var entityInstanceIndex = 0; entityInstanceIndex < instanceCount; ++entityInstanceIndex)
            {
                var unweightedResultsForInstance = unweightedResults[entityInstanceIndex];
                var unweightedSeries = unweightedResultsForInstance?.CellsTotalsSeries;
                var resultCount = unweightedSeries?.Count ?? 0;
                results[entityInstanceIndex] = new EntityWeightedTotalSeries(
                    unweightedResults[entityInstanceIndex].EntityInstance,
                    resultCount);
                var weightedResultsForInstance = results[entityInstanceIndex].Series;

                if (resultCount == 0)
                {
                    continue;
                }

                if (numberOfResponsesInSelectedQuotaCellsByIndex == null)
                {
                    numberOfResponsesInSelectedQuotaCellsByIndex = new int?[resultCount];
                }

                for (int resultIndex = 0; resultIndex < resultCount; ++resultIndex)
                {
                    var unweightedForDay = unweightedSeries[resultIndex];

                    var currentDate = unweightedForDay.Date;
                    var weightedTotalsForDay = CreateEmptyWeightedResults(currentDate, unweightedForDay.CellResultsWithSample.FirstOrDefault(x => x.TotalForAverage != null)?.TotalForAverage);

                    var periodWeights = weightsByResultIndex[resultIndex] ??= WeightGeneratorForRequestedPeriod.Generate(datasetSelector, _profileResponseAccessor, _quotaCellReferenceWeightingRepository, average, indexOrderedDesiredQuotaCells, currentDate);

                    foreach (var (quotaCell, weightingForQuotaCellForDay) in periodWeights)
                    {
                        if (unweightedForDay[quotaCell] is { TotalForAverage: { SampleSize: > 0 } unweightedResult } unweightedForQuotaCell)
                        {
                            AddUnweighted(unweightedResult, weightedTotalsForDay, weightingForQuotaCellForDay);

                            var responseIds = unweightedForQuotaCell.ResponseIdsForAverage;
                            if (responseIds != null)
                            {
                                weightedTotalsForDay.ResponseIdsForDay.AddRange(
                                    responseIds);
                            }
                        }
                    }
                    weightedResultsForInstance.Add(weightedTotalsForDay);
                }
            }

            return results;
        }

        private static WeightedTotal CreateEmptyWeightedResults(DateTimeOffset currentDate, ResultSampleSizePair unweightedForDay) =>
            new(currentDate)
            {
                ChildResults = unweightedForDay?.ChildResults?.Select(c => CreateEmptyWeightedResults(currentDate, c))
                    .ToArray(),
            };

        private static void AddUnweighted(ResultSampleSizePair unweightedResult,
            WeightedTotal weightedTotalsFor,
            double weightingForQuotaCellForDay)
        {
            weightedTotalsFor.WeightedValueTotal +=
                unweightedResult.Result * weightingForQuotaCellForDay;

            weightedTotalsFor.UnweightedValueTotal +=
                unweightedResult.Result;

            weightedTotalsFor.UnweightedSampleCount +=
                unweightedResult.SampleSize;

            weightedTotalsFor.WeightedSampleCount +=
                unweightedResult.SampleSize * weightingForQuotaCellForDay;

            if (weightedTotalsFor.ChildResults is not null)
            {
                for (int i = 0; i < weightedTotalsFor.ChildResults.Length; i++)
                {
                    var unweightedChild = unweightedResult.ChildResults[i];
                    var weightedChild = weightedTotalsFor.ChildResults[i];
                    AddUnweighted(unweightedChild, weightedChild, weightingForQuotaCellForDay);
                }
            }
        }
    }
}
