using System.Threading;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Middleware;
using BrandVue.Models;
using BrandVue.Services.Interfaces;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Measures;

namespace BrandVue.Services
{
    public class WaveResultsProvider : IWaveResultsProvider
    {
        private readonly IRequestAdapter _requestAdapter;
        private readonly IConvenientCalculator _convenientCalculator;

        public WaveResultsProvider(
            IRequestAdapter requestAdapter,
            IConvenientCalculator convenientCalculator)
        {
            _requestAdapter = requestAdapter;
            _convenientCalculator = convenientCalculator;
        }

        public Task<WaveComparisonResults> GetWaveComparisonResults(CuratedResultsModel model,
            IEnumerable<CompositeFilterModel> waves,
            IEnumerable<CompositeFilterModel> breaks,
            string comparandWave,
            CancellationToken cancellationToken)
        {
            return GetWaveComparisonResults(model.FilterModel,
                waves,
                breaks,
                model.IncludeSignificance,
                model.SigConfidenceLevel,
                comparandWave,
                (filter) => _requestAdapter.CreateParametersForCalculation(model, filter), cancellationToken);
        }

        public Task<WaveComparisonResults> GetWaveComparisonResults(MultiEntityRequestModel model,
            IEnumerable<CompositeFilterModel> waves,
            IEnumerable<CompositeFilterModel> breaks,
            string comparandWave,
            CancellationToken cancellationToken)
        {
            return GetWaveComparisonResults(model.FilterModel,
                waves,
                breaks,
                model.IncludeSignificance,
                model.SigConfidenceLevel,
                comparandWave,
                (filter) => _requestAdapter.CreateParametersForCalculation(model, filter), cancellationToken);
        }

        private async Task<WaveComparisonResults> GetWaveComparisonResults(CompositeFilterModel filterModel,
            IEnumerable<CompositeFilterModel> waves,
            IEnumerable<CompositeFilterModel> breaks,
            bool includeSignificance,
            SigConfidenceLevel sigConfidenceLevel,
            string comparandWave,
            Func<CompositeFilterModel, ResultsProviderParameters> createParametersForCalculation,
            CancellationToken cancellationToken)
        {
            ResultsPerWave[] comparisonResults;

            if (breaks.Any())
            {
                comparisonResults = await breaks.ToAsyncEnumerable().SelectAwait(async breakFilter => await GetResultsPerWave(filterModel, createParametersForCalculation, waves, cancellationToken, breakFilter)).ToArrayAsync(cancellationToken);
            }
            else
            {
                comparisonResults = [await GetResultsPerWave(filterModel, createParametersForCalculation, waves, cancellationToken)];
            }

            string errorMessage = string.Empty;
            if (includeSignificance)
            {
                try
                {
                    if (string.IsNullOrEmpty(comparandWave))
                    {
                        comparandWave = waves.Last().Name;
                    }

                    var primaryMeasure = GetPrimaryMeasure(filterModel, createParametersForCalculation);
                    MutateResultsToIncludeSignificance(comparisonResults, comparandWave, primaryMeasure, sigConfidenceLevel);
                }
                catch (Exception e)
                {
                    errorMessage = e.Message;
                }
            }

            var sampleSizeMeta = comparisonResults.GetSampleSizeMetadata();
            return new WaveComparisonResults
            {
                ComparisonResults = comparisonResults,
                HasData = comparisonResults.Any(r => r.WaveResults.Any(w => w.EntityResults.HasData())),
                SampleSizeMetadata = sampleSizeMeta,
                LowSampleSummary = sampleSizeMeta.SampleSizeByEntity
                    .Where(sampleForWaveAndBreak => sampleForWaveAndBreak.Value.Unweighted <= LowSampleExtensions.LowSampleThreshold)
                    .Select(sampleForWaveAndBreak => new LowSampleSummary { Name = sampleForWaveAndBreak.Key })
                    .ToArray(),
                ErrorMessage = errorMessage
            };
        }

        private static Measure GetPrimaryMeasure(CompositeFilterModel filterModel, Func<CompositeFilterModel, ResultsProviderParameters> createParametersForCalculation)
        {
            var pam = createParametersForCalculation(filterModel);
            return pam.PrimaryMeasure;
        }

        private async Task<ResultsPerWave> GetResultsPerWave(CompositeFilterModel filterModel,
            Func<CompositeFilterModel, ResultsProviderParameters> createParametersForCalculation,
            IEnumerable<CompositeFilterModel> waves,
            CancellationToken cancellationToken,
            CompositeFilterModel additionalFilter = null)
        {
            var resultsPerWave = await waves.ToAsyncEnumerable().SelectAwait(async waveFilter =>
            {
                var filters = additionalFilter == null ? [filterModel, waveFilter] : new[] { filterModel, waveFilter, additionalFilter };
                var combinedFilterModel = new CompositeFilterModel(FilterOperator.And, Enumerable.Empty<MeasureFilterRequestModel>(), filters);
                var pam = createParametersForCalculation(combinedFilterModel);
                return new WaveResult
                {
                    WaveName = waveFilter.Name,
                    EntityResults = (await _convenientCalculator.GetCuratedResultsForAllMeasures(pam, cancellationToken)).Single().Data,
                };
            }).ToArrayAsync(cancellationToken);
            return new ResultsPerWave()
            {
                BreakName = additionalFilter?.Name,
                WaveResults = resultsPerWave
            };
        }

        private static void MutateResultsToIncludeSignificance(IEnumerable<ResultsPerWave> waves,
            string comparandName,
            Measure primaryMeasure,
            SigConfidenceLevel sigConfidenceLevel)
        {
            foreach (var wave in waves)
            {
                for (var entityIndex = 0; entityIndex < wave.WaveResults.First().EntityResults?.Length; entityIndex++)
                {
                    foreach (var waveResult in wave.WaveResults)
                    {
                        var entityResult = waveResult.EntityResults[entityIndex].WeightedDailyResults.Single();

                        var comparandWave = wave.WaveResults.SingleOrDefault(w => w.WaveName == comparandName);
                        if (comparandWave is null)
                        {
                            throw new SignificanceCalculationException($"Unable to calculate significance as data for wave {comparandName} cannot be found");
                        }

                        var comparandInstance = comparandWave.EntityResults[entityIndex].WeightedDailyResults.Single();
                        
                        PipelineResultsProvider.MutateResultToIncludeSignificance(primaryMeasure, entityResult, comparandInstance, comparandName, sigConfidenceLevel);
                    }
                }
            }
        }
    }
}