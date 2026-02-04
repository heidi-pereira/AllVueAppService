using BrandVue.Services;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Entity;

namespace BrandVue.Models;

public class AverageResultWithPrevious : AbstractCommonResultsInformation
{
    public WeightedDailyResult CurrentWeightedDailyResult { get; }
    public WeightedDailyResult PreviousWeightedDailyResult { get; }
    
    public AverageResultWithPrevious(ResultsProviderParameters model,
        OverTimeAverageResults results)
    {
        var currentPeriodEndDate = model.CalculationPeriod.EndDate;
        var previousPeriodEndDate = model.CalculationPeriod.Periods.First().EndDate;
        HasData = results.HasData;
        LowSampleSummary = results.LowSampleSummary;
        SampleSizeMetadata = results.SampleSizeMetadata;
        TrialRestrictedData = results.TrialRestrictedData;
        CurrentWeightedDailyResult =
            results.WeightedDailyResults.SingleOrDefault(x => x.Date == currentPeriodEndDate);
        PreviousWeightedDailyResult =
            results.WeightedDailyResults.SingleOrDefault(x => x.Date == previousPeriodEndDate);
    }
}