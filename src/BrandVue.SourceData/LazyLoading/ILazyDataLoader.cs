using System.Threading;
using System.Threading.Tasks;
using BrandVue.SourceData.CommonMetadata;

namespace BrandVue.SourceData.LazyLoading
{
    public interface ILazyDataLoader
    {
        Task<EntityMetricData[]> GetDataForFields(Subset subset, IReadOnlyCollection<ResponseFieldDescriptor> fields,
            (DateTime startDate, DateTime endDate)? timeRange, IReadOnlyCollection<IDataTarget> targetInstances,
            CancellationToken cancellationToken);
        IEnumerable<ResponseFieldData> GetResponses(Subset subset, IReadOnlyCollection<ResponseFieldDescriptor> quotaCellFields);
        IDataLimiter DataLimiter { get; }
    }
}