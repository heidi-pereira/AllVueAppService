using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline
{
    public static class WeightGeneratorForRequestedPeriod
    {
        /// <returns>Dictionary containing weights for all non-zero weighted quota cells requested</returns>
        public static Dictionary<QuotaCell, double> Generate(Subset subset,
            IProfileResponseAccessor profileResponseAccessor,
            IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository,
            AverageDescriptor averageDescriptor, IGroupedQuotaCells indexOrderedQuotaCells,
            DateTimeOffset endOfThePeriodDate)
        {

            return indexOrderedQuotaCells.IndependentlyWeightedGroups.Values.SelectMany(g =>
                GenerateInner(subset, profileResponseAccessor, quotaCellReferenceWeightingRepository, averageDescriptor, g, endOfThePeriodDate)
            ).ToDictionary(w => w.QuotaCell, w => w.WeightingForQuotaCellForDay);
        }

        /// <returns>Dictionary containing weights for all non-zero weighted quota cells requested</returns>
        private static IEnumerable<(QuotaCell QuotaCell, double WeightingForQuotaCellForDay)> GenerateInner(Subset subset,
            IProfileResponseAccessor profileResponseAccessor,
            IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository,
            AverageDescriptor averageDescriptor, IGroupedQuotaCells indexOrderedQuotaCells,
            DateTimeOffset endOfThePeriodDate)
        {
            if (averageDescriptor.WeightingMethod == WeightingMethod.None || !indexOrderedQuotaCells.Any())
            {
                return indexOrderedQuotaCells.Cells.Select(q => (q, 1.0));
            }

            var optionalStartDateFilter = averageDescriptor.TotalisationPeriodUnit switch
            {
                TotalisationPeriodUnit.Day => endOfThePeriodDate.AddDays(1 - averageDescriptor.NumberOfPeriodsInAverage),
                TotalisationPeriodUnit.Month when averageDescriptor.WeightingPeriodUnit == WeightingPeriodUnit.FullScheme => DateTimeOffset.MinValue,
                TotalisationPeriodUnit.Month => endOfThePeriodDate.AddDays(1 - endOfThePeriodDate.Day),
                TotalisationPeriodUnit.All => DateTimeOffset.MinValue, // All
                _ => throw new ArgumentOutOfRangeException(nameof(averageDescriptor), averageDescriptor, null)
            };
            var optionalEndDateFilter = averageDescriptor.WeightingPeriodUnit == WeightingPeriodUnit.SameAsTotalization ? endOfThePeriodDate : DateTimeOffset.MaxValue;

            return GenerateCellWeights(subset, profileResponseAccessor, quotaCellReferenceWeightingRepository, indexOrderedQuotaCells, optionalStartDateFilter, optionalEndDateFilter);
        }

        internal static IEnumerable<(QuotaCell QuotaCell, double WeightingForQuotaCellForDay)> GenerateCellWeights(Subset subset, 
            IProfileResponseAccessor profileResponseAccessor,
            IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository,
            IGroupedQuotaCells indexOrderedQuotaCells,
            DateTimeOffset optionalStartDateFilter,
            DateTimeOffset optionalEndDateFilter)
        {
            var allCellResponseCount = profileResponseAccessor.GetResponses(indexOrderedQuotaCells.Unfiltered)
                .WithinTimesInclusive(optionalStartDateFilter, optionalEndDateFilter).Sum(c => c.Profiles.Length);
            var perCellResponses = profileResponseAccessor.GetResponses(indexOrderedQuotaCells)
                .WithinTimesInclusive(optionalStartDateFilter, optionalEndDateFilter);

            var quotaCellCountsForPeriod =
                perCellResponses.Select(qcp => (qcp.QuotaCell, ProfileCount: qcp.Profiles.Length)).ToArray();

            var nonZeroPeriodWeightsByQuotaCell = quotaCellCountsForPeriod.Select(qcp =>
            {
                (var quotaCell, int profileCount) = qcp;
                var targetReferenceWeighting =
                    quotaCellReferenceWeightingRepository.Get(subset)
                    .GetReferenceWeightingFor(quotaCell);

                var weightingForQuotaCellForDay = 1.0;
                if (targetReferenceWeighting.Weight.HasValue)
                {
                    if (targetReferenceWeighting.IsResponseLevelWeighting)
                    {
                        weightingForQuotaCellForDay = targetReferenceWeighting.Weight.Value;
                    }
                    else
                    {
                        weightingForQuotaCellForDay = allCellResponseCount * targetReferenceWeighting.Weight.Value
                                                      / profileCount;
                        //Never 0 because quota cells with 0 profiles aren't returned
                    }
                }
                if (targetReferenceWeighting.ExpansionFactor.HasValue)
                {
                    weightingForQuotaCellForDay *= targetReferenceWeighting.ExpansionFactor.Value;
                }
                return (qcp.QuotaCell, weightingForQuotaCellForDay);
            });

            return nonZeroPeriodWeightsByQuotaCell;
        }

        internal static IEnumerable<ResponseWeight> ResponseWeightsForDay(IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository, Subset datasetSelector, AverageDescriptor average, IGroupedQuotaCells quotaCells,
            IProfileResponseAccessor profileResponseAccessor, CellTotals total)
        {
            var quotaWeights = Generate(datasetSelector, profileResponseAccessor,
                quotaCellReferenceWeightingRepository, average, quotaCells, total.Date);
            return quotaWeights
                .Where(quotaWeight => total[quotaWeight.Key] != null)
                .SelectMany(quotaWeight =>
                    total[quotaWeight.Key].ResponseIdsForAverage.Select(responseId => new ResponseWeight(responseId, quotaWeight.Value)));
        }
    }
}
