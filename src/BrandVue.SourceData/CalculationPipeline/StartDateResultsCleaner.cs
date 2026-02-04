using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.CommonMetadata;

namespace BrandVue.SourceData.CalculationPipeline
{
    public class StartDateResultsCleaner
    {
        public void RemoveResultsBeforeStartDate(
            EntityWeightedDailyResults results,
            Subset subset,
            AverageDescriptor avgDescriptor)
        {
            if (results.EntityInstance == null)
            {
                return;
            }
            var startDate = results.EntityInstance.StartDateForSubset(subset.Id) ?? DateTimeOffset.MinValue;
            MutateResultsBeforeStartDate(results, avgDescriptor, startDate);
        }

        public void RemoveResultsBeforeStartDateForTargetInstances(
            EntityWeightedDailyResults results,
            Subset subset,
            AverageDescriptor avgDescriptor,
            TargetInstances[] instances)
        {
            if (instances == null || instances.Length == 0)
            {
                return;
            }

            var startDate = instances.Max(instance => instance.OrderedInstances.Max(entityInstance =>
                entityInstance.StartDateForSubset(subset.Id) ?? DateTimeOffset.MinValue));
            MutateResultsBeforeStartDate(results, avgDescriptor, startDate);
        }

        private static void MutateResultsBeforeStartDate(EntityWeightedDailyResults results, AverageDescriptor avgDescriptor, DateTimeOffset startDate)
        {
            if (startDate == DateTimeOffset.MinValue)
            {
                return;
            }

            if (avgDescriptor.TotalisationPeriodUnit == TotalisationPeriodUnit.Day)
            {
                startDate = startDate
                    .AddDays(avgDescriptor.NumberOfPeriodsInAverage - 1)
                    .ToDateInstance();
            }

            foreach (var result in results.WeightedDailyResults)
            {
                if (result.Date < startDate)
                {
                    result.UnweightedSampleSize = 0;
                    result.StandardDeviation = 0;
                    result.Variance = 0;
                    result.WeightedResult = 0.0;
                }
                else
                {
                    break;
                }
            }
        }
    }
}