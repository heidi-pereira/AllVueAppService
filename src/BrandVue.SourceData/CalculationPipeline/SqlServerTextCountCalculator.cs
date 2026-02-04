using System.Threading.Tasks;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.EntityFramework.Answers;

namespace BrandVue.SourceData.CalculationPipeline;

public class SqlServerTextCountCalculator : BaseTextCountCalculator
{
    private readonly IResponseRepository _textResponseRepository;

    public SqlServerTextCountCalculator(
        IProfileResponseAccessorFactory profileResponseAccessorFactory,
        IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository,
        IMeasureRepository measureRepository,
        IResponseRepository textResponseRepository,
        IAsyncTotalisationOrchestrator resultsCalculator)
        : base(profileResponseAccessorFactory, quotaCellReferenceWeightingRepository, measureRepository, resultsCalculator)
    {
        _textResponseRepository = textResponseRepository;
    }

    protected override Task<WeightedWordCount[]> GetWeightedTextCountsAsync(
        ResponseWeight[] responseWeights,
        string varCodeBase,
        IReadOnlyCollection<(DbLocation Location, int Id)> filters)
    {
        var result = _textResponseRepository.GetWeightedLoweredAndTrimmedTextCounts(
            responseWeights,
            varCodeBase,
            filters);

        return Task.FromResult(result);
    }
}