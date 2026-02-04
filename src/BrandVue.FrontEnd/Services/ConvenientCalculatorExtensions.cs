using System.Threading;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;

namespace BrandVue.Services;

/// <summary>
/// Put things here that are just even more convenient overloads. It makes mocking the convenient calculator easier.
/// </summary>
public static class ConvenientCalculatorExtensions
{
    public record SingleDataPointPerEntityMeasure(Measure Measure, (EntityInstance EntityInstance, WeightedDailyResult WeightedResult)[] Data);
    public static async Task<IEnumerable<SingleDataPointPerEntityMeasure>> CoalesceSingleDataPointPerEntityMeasure(
        this IConvenientCalculator convenientCalculator, ResultsProviderParameters calculationParameters,
        CancellationToken cancellationToken)
    {
        return (await convenientCalculator.GetCuratedResultsForAllMeasures(calculationParameters, cancellationToken)).Select(results =>
        {
            var overallResultPerRequestedInstance = results.Data
                .Select(d => (
                    EntityInstance: d.EntityInstance ??
                                    GetFakeProfileEntityInstance(calculationParameters.PrimaryMeasure),
                    WeightedResult: d.WeightedDailyResults.SingleOrDefault() ??
                                    new WeightedDailyResult(DateTimeOffset.Now)
                                        { UnweightedSampleSize = 0 }))
                .ToArray();
            return new SingleDataPointPerEntityMeasure(Measure: results.Measure, Data: overallResultPerRequestedInstance);
        });

        EntityInstance GetFakeProfileEntityInstance(Measure primaryMeasure) =>
            new EntityInstance
            {
                Name = primaryMeasure.DisplayName
            };
    }
}