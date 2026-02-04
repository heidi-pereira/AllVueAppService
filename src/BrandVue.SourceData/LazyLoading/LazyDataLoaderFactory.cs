using BrandVue.SourceData.CalculationPipeline;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.LazyLoading
{
    public class LazyDataLoaderFactory : ILazyDataLoaderFactory
    {
        private readonly ISqlProvider _sqlProvider;

        public LazyDataLoaderFactory(ISqlProvider sqlProvider)
        {
            _sqlProvider = sqlProvider;
        }

        public ILazyDataLoader Build(DateTimeOffset? lastSignOffDate, ILogger logger, IProductContext productContext, int[] surveyIds)
        {
            var syncedDataLimiter = surveyIds.Any() ? new SyncedDataLimiter(_sqlProvider, productContext, surveyIds, lastSignOffDate, logger) : (IDataLimiter) new NullDataLimiter();
            return new AnswersTableLazyDataLoader(_sqlProvider, syncedDataLimiter);
        }
    }
}