using System.Threading;
using BrandVue.PublicApi.Models;

namespace BrandVue.PublicApi.Services
{
    public interface IMetricResultCalculationProxy
    {
        Task<IEnumerable<MetricCalculationResult>> Calculate(MetricCalculationRequestInternal metricCalculationRequest,
            CancellationToken cancellationToken);
    }
}
