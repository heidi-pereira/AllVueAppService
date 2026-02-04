using System.Threading;
using BrandVue.Models;

namespace BrandVue.Services.Interfaces
{
    public interface IWaveResultsProvider
    {
         Task<WaveComparisonResults> GetWaveComparisonResults(CuratedResultsModel model,
            IEnumerable<CompositeFilterModel> waves, IEnumerable<CompositeFilterModel> breaks, string comparandName,
            CancellationToken cancellationToken);
        Task<WaveComparisonResults> GetWaveComparisonResults(MultiEntityRequestModel model,
            IEnumerable<CompositeFilterModel> waves, IEnumerable<CompositeFilterModel> breaks, string comparandName,
            CancellationToken cancellationToken);
    }
}