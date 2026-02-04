using System.Threading;
using BrandVue.Models;
using BrandVue.SourceData;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.Services
{
    public class BreakdownResultsProvider : IBreakdownResultsProvider
    {
        private readonly IConvenientCalculator _convenientCalculator;
        private readonly IBreakdownCategoryFactory _breakdownCategoryFactory;
        private readonly IMetricCalculationOrchestrator _calculator;
        private readonly IProfileResponseAccessorFactory _profileResponseAccessorFactory;

        public BreakdownResultsProvider(IConvenientCalculator convenientCalculator, IBreakdownCategoryFactory breakdownCategoryFactory, IMetricCalculationOrchestrator calculator, IProfileResponseAccessorFactory profileResponseAccessorFactory)
        {
            _convenientCalculator = convenientCalculator;
            _breakdownCategoryFactory = breakdownCategoryFactory;
            _calculator = calculator;
            _profileResponseAccessorFactory = profileResponseAccessorFactory;
        }

        public async Task<BreakdownResults> GetBreakdown(ResultsProviderParameters pam,
            DemographicFilter demographicFilter, CancellationToken cancellationToken)
        {
            var unweighted = await _convenientCalculator.CalculateUnweightedForMeasure(pam, cancellationToken);

            var byAgeGroup = await _convenientCalculator.WeightCategoryWithoutSignificance(unweighted, _breakdownCategoryFactory.ByAgeGroup(demographicFilter, pam.Subset), cancellationToken);

            var byGender = await _convenientCalculator.WeightCategoryWithoutSignificance(unweighted, _breakdownCategoryFactory.ByGender(demographicFilter, pam.Subset), cancellationToken);

            var byRegion = await _convenientCalculator.WeightCategoryWithoutSignificance(unweighted, _breakdownCategoryFactory.ByRegion(demographicFilter, pam.Subset), cancellationToken);

            var segCategory = _breakdownCategoryFactory.BySegOrNull(demographicFilter, pam.Subset);
            var bySeg = segCategory == null ? null : await _convenientCalculator.WeightCategoryWithoutSignificance(unweighted, segCategory, cancellationToken);

            var weighted = await _calculator.CalculateWeightedFromUnweighted(unweighted, calculateSignificance: pam.IncludeSignificance, cancellationToken);

            var brokenDownResults = new BrokenDownResults[weighted.Length];
            for (var index = 0; index < weighted.Length; ++index)
            {
                brokenDownResults[index]
                    = new BrokenDownResults(
                        pam.PrimaryMeasure,
                        weighted[index].EntityInstance,
                        byAgeGroup[index].Results,
                        byGender[index].Results,
                        byRegion[index].Results,
                        bySeg?[index].Results,
                        weighted[index].WeightedDailyResults);
            }

            brokenDownResults = brokenDownResults.OrderByFocusEntityInstanceAndThenAlphabeticByEntityInstanceName(pam.FocusEntityInstanceId);

            brokenDownResults =
                brokenDownResults.OrderByFocusEntityInstanceAndThenAlphabeticByEntityInstanceName(
                    pam.FocusEntityInstanceId);

            var sampleSizeWeightedDailyResults =
                pam.SampleSizeEntityInstanceId.HasValue
                    ? brokenDownResults.SingleOrDefault(r =>
                            r.EntityInstance == null || r.EntityInstance.Id == pam.SampleSizeEntityInstanceId)?.Total
                        ?.ToArray()
                    : brokenDownResults.FirstOrDefault()?.Total?.ToArray();

            return new BreakdownResults
            {
                Data = brokenDownResults,
                SampleSizeMetadata = sampleSizeWeightedDailyResults.GetSampleSizeMetadata(),
                HasData = brokenDownResults.HasData(),
                LowSampleSummary = pam.DoMeasuresIncludeMarketMetric ? new LowSampleSummary[] { } : brokenDownResults.LowSampleSummaries(pam.Subset.Id, _profileResponseAccessorFactory.GetOrCreate(pam.Subset).StartDate)
            };
        }
    }
}