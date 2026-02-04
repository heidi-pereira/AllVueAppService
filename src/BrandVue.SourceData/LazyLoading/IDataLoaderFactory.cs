using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.LazyLoading
{
    public interface ILazyDataLoaderFactory
    {
        ILazyDataLoader Build(DateTimeOffset? lastSignOffDate, ILogger logger, IProductContext productContext, int[] surveyIds = null);
    }
}