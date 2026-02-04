using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.Measures
{
    public delegate Task<UnweightedTotals> CalculateUnweightedResults(Subset subset,
        CalculationPeriod calculationPeriod,
        AverageDescriptor average,
        Measure measure,
        TargetInstances requestedInstances,
        IGroupedQuotaCells quotaCells,
        TargetInstances[] filterInstances,
        IFilter filter,
        EntityWeightedDailyResults[] weightedAverages = null);
    
    public static class EatingOutMarketMetricsHelper
    {
        private static readonly CalculationType[] EatingOutMarketMetricCalculationTypes = {
            CalculationType.EoTotalSpendPerTimeOfDay,
            CalculationType.EoTotalSpendPerLocation
        };

        private const int UkAdultPopulationSize = 45000000;

        public static bool IsEatingOutMarketMetric(CalculationType calculationType)
        {
            return EatingOutMarketMetricCalculationTypes.Contains(calculationType);
        }
        
        public static Task<UnweightedTotals> CalculateEoMarketMetric(Subset subset,
            CalculationPeriod calculationPeriod, AverageDescriptor average, Measure measure, IGroupedQuotaCells quotaCells,
            IFilter filter, TargetInstances[] filterInstances, TargetInstances requestedInstances,
            IMeasureRepository measureRepo, CalculateUnweightedResults calculateUnweighted)
        {
            if (!IsEatingOutMarketMetric(measure.CalculationType))
                throw new ArgumentException("Invalid metric used! Eating Out market metrics logic can only be used on metrics with special EO calculation types.");
            if (!subset.Id.Equals("uk", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Invalid subset used! Eating Out market metrics only support UK subset.");
            if (average.MakeUpTo != MakeUpTo.MonthEnd || average.NumberOfPeriodsInAverage != 1)
                throw new ArgumentException("Invalid average used! Eating Out market metrics only support monthly averages. Other average types have not been tested.");

            switch (measure.CalculationType)
            {
                case CalculationType.EoTotalSpendPerTimeOfDay:
                    return CalculateEoTotalSpendPerTimeOfDay(subset, calculationPeriod, average, measure, quotaCells,
                        filter, filterInstances, requestedInstances, measureRepo, calculateUnweighted);
                case CalculationType.EoTotalSpendPerLocation:
                    return CalculateEoTotalSpendPerLocation(subset, calculationPeriod, average, measure, quotaCells,
                        filter, filterInstances, requestedInstances, measureRepo, calculateUnweighted);
                default:
                    throw new ArgumentOutOfRangeException($"Unexpected measure used, calcType={measure.CalculationType}");
            }
        }

        private static Measure GetMeasureAndApplyOverridesIfNeeded(IMeasureRepository measureRepository, string targetMeasureName, Measure measureWithPotentialOverrides)
        {
            var targetMeasure = measureRepository.Get(targetMeasureName).ShallowCopy();
            // target measure should always respect start date set in overrides measure
            targetMeasure.StartDate = measureWithPotentialOverrides.StartDate;
            return targetMeasure;
        }
        
        private static async Task<UnweightedTotals> CalculateEoTotalSpendPerTimeOfDay(Subset subset,
            CalculationPeriod calculationPeriod, AverageDescriptor average, Measure measure, IGroupedQuotaCells quotaCells,
            IFilter filter, TargetInstances[] filterInstances, TargetInstances requestedInstances,
            IMeasureRepository measureRepository, CalculateUnweightedResults calculateUnweighted)
        {
            var averageSpendPerTimeOfDayMetric = GetMeasureAndApplyOverridesIfNeeded(measureRepository, "Yesterday spend", measure);
            var eatingOutAtTimeOfDayMetric = GetMeasureAndApplyOverridesIfNeeded(measureRepository, "YesterdayEatingOutPerTimeOfDay", measure);

            var spendResults = await calculateUnweighted(subset,
                calculationPeriod,
                average,
                averageSpendPerTimeOfDayMetric,
                requestedInstances,
                quotaCells,
                filterInstances,
                filter);
            
            var eatingOutAtTimeOfDayResults = await calculateUnweighted(subset, 
                calculationPeriod,
                average,
                eatingOutAtTimeOfDayMetric,
                requestedInstances,
                quotaCells,
                filterInstances,
                filter);

            for (int timeOfDayIndex = 0; timeOfDayIndex < spendResults.Unweighted.Length; timeOfDayIndex++)
            {
                var spendResultsForTimeOfDay = spendResults.Unweighted[timeOfDayIndex];
                var eatingOutResultsForTimeOfDay = eatingOutAtTimeOfDayResults.Unweighted[timeOfDayIndex];
                
                for (int monthResultIndex = 0; monthResultIndex < spendResultsForTimeOfDay.CellsTotalsSeries.Count; monthResultIndex++)
                {
                    var monthSpendResults = spendResultsForTimeOfDay.CellsTotalsSeries[monthResultIndex];
                    var monthEatingOutResults = eatingOutResultsForTimeOfDay.CellsTotalsSeries[monthResultIndex];
                    var daysInMonthCount = monthSpendResults.Date.GetNumberOfDaysInMonth();
                    
                    foreach(var quotaCell in spendResults.QuotaCells.Cells)
                    {
                        if (monthEatingOutResults[quotaCell] == null || monthSpendResults[quotaCell] == null)
                            continue;

                        var totalQuotaCellCount = monthEatingOutResults[quotaCell].TotalForAverage.SampleSize;
                        var avgSpend = GetAverageFromResultSafely(monthSpendResults[quotaCell].TotalForAverage);
                        var peopleWhoAteOutCount = monthEatingOutResults[quotaCell].TotalForAverage.Result;

                        var totalSpend = avgSpend * peopleWhoAteOutCount;
                        var totalSpendScaledToPopulation = totalSpend * daysInMonthCount * UkAdultPopulationSize;
                        var scaleFactorOrDefault = measure.ScaleFactor ?? 1.0;
                        var totalSpendScaledToPopulationAndScaleFactor = totalSpendScaledToPopulation * scaleFactorOrDefault;
                        monthSpendResults[quotaCell].TotalForAverage = new ResultSampleSizePair { Result = totalSpendScaledToPopulationAndScaleFactor, SampleSize = totalQuotaCellCount };
                    }
                }
            }

            return spendResults;
        }

        private static async Task<UnweightedTotals> CalculateEoTotalSpendPerLocation(Subset subset,
            CalculationPeriod calculationPeriod, AverageDescriptor average, Measure measure, IGroupedQuotaCells quotaCells,
            IFilter filter, TargetInstances[] filterInstances, TargetInstances requestedInstances,
            IMeasureRepository measureRepository, CalculateUnweightedResults calculateUnweighted)
        {
            var eatingOutYesterdayMetric = GetMeasureAndApplyOverridesIfNeeded(measureRepository, "EatingOutYesterday", measure);
            var eatingOutAtLocationMetric = GetMeasureAndApplyOverridesIfNeeded(measureRepository, "All locations yesterday", measure);
            var spendAtLocationMetric = GetMeasureAndApplyOverridesIfNeeded(measureRepository, "Average spend", measure);
            var eoOccasionsWeDontKnowAboutMetric = GetMeasureAndApplyOverridesIfNeeded(measureRepository, "EatingOutOccasionsWeDontKnowAbout", measure);
            
            var eatingOutYesterdayResults = await calculateUnweighted(subset,
                calculationPeriod,
                average,
                eatingOutYesterdayMetric,
                requestedInstances,
                quotaCells,
                filterInstances,
                filter);
            
            var knownOccasionsAtLocationResults = await calculateUnweighted(subset, 
                calculationPeriod,
                average,
                eatingOutAtLocationMetric,
                requestedInstances,
                quotaCells,
                filterInstances,
                filter);
            
            var spendAtLocationResults = await calculateUnweighted(subset, 
                calculationPeriod,
                average,
                spendAtLocationMetric,
                requestedInstances,
                quotaCells,
                filterInstances,
                filter);
            
            var unknownOccasionsResults = await calculateUnweighted(subset, 
                calculationPeriod,
                average,
                eoOccasionsWeDontKnowAboutMetric,
                requestedInstances,
                quotaCells,
                filterInstances,
                filter);
            
            for (int locationIndex = 0; locationIndex < knownOccasionsAtLocationResults.Unweighted.Length; locationIndex++)
            {
                var knownOccasionsResultsForLocation = knownOccasionsAtLocationResults.Unweighted[locationIndex];
                var spendResultsForLocation = spendAtLocationResults.Unweighted[locationIndex];

                //These metrics do not depend on location but since we used the same targetinstances,
                //we do have per location instance results which are all identical
                var eatingOutResultsForLocation = eatingOutYesterdayResults.Unweighted[locationIndex];
                var unknownOccasionsResultsForLocation = unknownOccasionsResults.Unweighted[locationIndex];
                
                for (int monthResultIndex = 0; monthResultIndex < knownOccasionsResultsForLocation.CellsTotalsSeries.Count; monthResultIndex++)
                {
                    var monthEatingOutResults = eatingOutResultsForLocation.CellsTotalsSeries[monthResultIndex];
                    var monthKnownOccasionsResults = knownOccasionsResultsForLocation.CellsTotalsSeries[monthResultIndex];
                    var monthAvgSpendResults = spendResultsForLocation.CellsTotalsSeries[monthResultIndex];
                    var monthUnknownOccasionsResults = unknownOccasionsResultsForLocation.CellsTotalsSeries[monthResultIndex];
                    var daysInMonthCount = monthKnownOccasionsResults.Date.GetNumberOfDaysInMonth();
                    
                    foreach(var quotaCell in eatingOutYesterdayResults.QuotaCells.Cells)
                    {
                        if (monthEatingOutResults[quotaCell] == null || monthKnownOccasionsResults[quotaCell] == null)
                            continue;
                        
                        var peopleAskedAboutEatingOutCount = monthEatingOutResults[quotaCell].TotalForAverage.SampleSize;
                        
                        var knownOccasionsAtLocationCount = monthKnownOccasionsResults[quotaCell].TotalForAverage.Result;
                        if (knownOccasionsAtLocationCount <= 0)
                        {
                            //If no respondents declared eating out at a given location, then the likelihood for choosing this location for unknown occasions is also 0.
                            //Consequently, this quota cells contribution to the total spend per location is 0. We can skip the rest of this loop for performance reasons.
                            monthKnownOccasionsResults[quotaCell].TotalForAverage = new ResultSampleSizePair { Result = 0.0, SampleSize = peopleAskedAboutEatingOutCount };
                            continue;
                        }
                        
                        var likelihoodToChooseLocation = GetAverageFromResultSafely(monthKnownOccasionsResults[quotaCell].TotalForAverage);
                        var unknownOccasionsCount = monthUnknownOccasionsResults[quotaCell].TotalForAverage.Result;
                        var totalOccasionsCount = knownOccasionsAtLocationCount + unknownOccasionsCount * likelihoodToChooseLocation;

                        var avgSpend = GetAverageFromResultSafely(monthAvgSpendResults[quotaCell].TotalForAverage);
                        var totalSpend = totalOccasionsCount * avgSpend;
                        var totalSpendScaledToPopulation = totalSpend * daysInMonthCount * UkAdultPopulationSize;
                        var scaleFactorOrDefault = measure.ScaleFactor ?? 1.0;
                        var totalSpendScaledToPopulationAndScaleFactor = totalSpendScaledToPopulation * scaleFactorOrDefault;

                        monthKnownOccasionsResults[quotaCell].TotalForAverage = new ResultSampleSizePair { Result = totalSpendScaledToPopulationAndScaleFactor, SampleSize = peopleAskedAboutEatingOutCount };
                    }
                }
            }

            return knownOccasionsAtLocationResults;
        }

        private static double GetAverageFromResultSafely(ResultSampleSizePair result)
        {
            return result.SampleSize == 0 ? 0 
                : result.Result / result.SampleSize;
        }
    }
}