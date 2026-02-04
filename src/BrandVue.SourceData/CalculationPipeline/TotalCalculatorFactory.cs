using BrandVue.SourceData.Measures;

namespace BrandVue.SourceData.CalculationPipeline
{
    internal static class TotalCalculatorFactory
    {
        private static readonly AppSettings AppSettings = new();

        public static IFilteredMetricTotaliser Create(FilteredMetric filteredMetric,
            EntityType requestedInstancesEntityType)
        {
            var measure = filteredMetric.Metric;
            switch (measure.CalculationType)
            {
                case CalculationType.YesNo:
                case CalculationType.NetPromoterScore:
                case CalculationType.Average:
                    return AppSettings.UseOptimisedCrossbreakCalculations
                        ? SingleEntityOptimisedFilteredMetricTotaliser.CreateIfUsable(filteredMetric, requestedInstancesEntityType) ?? new FilteredMetricTotaliser()
                        : new FilteredMetricTotaliser();

                case CalculationType.Text:
                    return new ResponseIdCalculator();

                case CalculationType.Special_ShouldNotBeUsed:
                    throw new NotImplementedException(
                        $"No total calculator implemented for calculation type {measure.CalculationType}; unable to provide total calculator for measure {measure.Name}.");

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(measure.CalculationType),
                        measure.CalculationType,
                        $"Invalid calculation type {measure.CalculationType} for measure {measure.Name}. Unable to create total calculator.");
            }
        }

        public static ICalcTypeResponseValueTransformer Create(Measure measure)
        {
            switch (measure.CalculationType)
            {
                case CalculationType.YesNo:
                    return new YesNoTotalTransformer(measure);

                case CalculationType.NetPromoterScore:
                    return new NetPromoterScoreTotalTransformer(measure);

                case CalculationType.Average:
                    return new AverageTotalTransformer(measure);

                case CalculationType.Text:
                    throw new NotImplementedException(
                        $"No {nameof(ICalcTypeResponseValueTransformer)} calculator implemented for calculation type {measure.CalculationType}; unable to provide total calculator for measure {measure.Name}.");


                case CalculationType.Special_ShouldNotBeUsed:
                    throw new NotImplementedException(
                        $"No {nameof(ICalcTypeResponseValueTransformer)} implemented for calculation type {measure.CalculationType}; unable to provide total calculator for measure {measure.Name}.");

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(measure.CalculationType),
                        measure.CalculationType,
                        $"Invalid calculation type {measure.CalculationType} for measure {measure.Name}. Unable to create total calculator.");
            }
        }
    }
}
