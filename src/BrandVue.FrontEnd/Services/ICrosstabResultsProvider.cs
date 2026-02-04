using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.Models;
using System.Threading;

namespace BrandVue.Services
{
    public interface ICrosstabResultsProvider
    {
        Task<CrosstabResults[]> GetCrosstabResults(CrosstabRequestModel model, CancellationToken cancellationToken);
        Task<CrosstabulatedResults[]> ExperimentalCrosstabResults(TemporaryVariableRequestModel model, CancellationToken cancellationToken);
        IEnumerable<CompositeFilterModel> GetFlattenedBreaksForMeasure(CrossMeasure cm, string subsetId);
        IEnumerable<(string MeasureVarCode, IEnumerable<CompositeFilterModel> Filters)> GetGroupedFlattenedBreaks(CrossMeasure[] breaks, string subsetId);
        Task<CrosstabAverageResults> GetOverTimeAverageResultsWithBreaks(CuratedResultsModel model,
            CrossMeasure[] breaks, AverageType averageType, CancellationToken cancellationToken);
        Task<CrosstabAverageResults[]> GetAverageResultsWithBreaks(CrosstabRequestModel model, AverageType averageType,
            CancellationToken cancellationToken);
        Task<CrosstabAverageResults> GetAverageForMultiEntityCharts(AverageMultiEntityChartModel model,
            CancellationToken cancellationToken);
    }
}