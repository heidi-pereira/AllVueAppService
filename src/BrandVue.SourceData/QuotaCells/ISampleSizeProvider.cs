using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Measures;
using System.Threading;
using System.Threading.Tasks;

namespace BrandVue.SourceData.QuotaCells
{
    public record SampleSize(int EntityId, int SampleCount);
    public interface ISampleSizeProvider
    {
        Task<double> GetTotalSampleSize(Subset subset,
            IFilter filter,
            WeightingMetrics weightingMetrics,
            CancellationToken cancellationToken);
        
        public Task<IEnumerable<int>> GetRespondents(Subset subset,
            IFilter filter,
            WeightingMetrics weightingMetrics,
            CancellationToken cancellationToken);

        Task<ResultSampleSizePair> GetUnweightedProfileResultAndSample(Subset subset,
            Measure measure, IFilter filter,
            WeightingMetrics weightingMetrics,
            CancellationToken cancellationToken);

        Task<IEnumerable<(EntityInstance Instance, ResultSampleSizePair Result)>> GetUnweightedEntityResultAndSample(
            Subset subset,
            Measure measure,
            IFilter filter,
            WeightingMetrics weightingMetrics,
            CancellationToken cancellationToken);
        
        Task<IEnumerable<(EntityInstance Instance, ResultSampleSizePair Result)>>
            GetUnweightedEntityResultAndSampleMultiEntityEstimate(Subset subset,
            Measure measure,
            IFilter filter,
            WeightingMetrics weightingMetrics,
            CancellationToken cancellationToken);
        
        Task<IReadOnlyDictionary<EntityInstance, double>> GetSampleSizeByEntity(Subset subset,
            Measure metric,
            IFilter filter,
            WeightingMetrics weightingMetrics,
            CancellationToken cancellationToken);
        
        IReadOnlyDictionary<EntityInstance, double> GetSampleSizeByEntityUsingCurrentWeighting(Subset subset,
            Measure metric,
            IFilter filter,
            WeightingMetrics
            weightingMetrics);

        Task<IReadOnlyList<(QuotaCell QuotaCell, double SampleSize)>> GetSampleSizeByQuotaCell(Subset subset,
            WeightingMetrics weightingMetrics,
            CancellationToken cancellationToken);
        
        Task<IReadOnlyList<(QuotaCell QuotaCell, double SampleSize)>> GetSampleSizeByQuotaCell(Subset subset,
            IFilter filter,
            WeightingMetrics weightingMetrics,
            CancellationToken cancellationToken);

        public Task<IList<SampleSize>> GetSampleSizeByWeightingForTopLevel(Subset subset,
            WeightingMetrics weightingMetrics,
            CancellationToken cancellationToken);

    }
}