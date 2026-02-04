using System.Threading;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.Models;
using BrandVue.SourceData;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Models;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;

namespace BrandVue.Services
{
    public interface IConvenientCalculator
    {
        Task<ResultsForMeasure[]> GetCuratedResultsForAllMeasures(ResultsProviderParameters pam, CancellationToken cancellationToken);
        
        Task<(Measure Measure, IList<WeightedDailyResult> AveragedResults, EntityWeightedDailyResults[] PerEntityInstanceResults)[]>
            GetCuratedMarketAverageResultsForAllMeasures(ResultsProviderParameters pam, CancellationToken cancellationToken);
        
        Task<(Measure Measure, EntityWeightedDailyResults[] PerEntityInstanceResults)[]>
            GetCuratedMarketResultsForAllMeasures(ResultsProviderParameters pam, CancellationToken cancellationToken);
        
        Task<(Measure Measure, WeightedDailyResult WeightedDailyResult)[]> GetAverageMentionsResultForAllMeasures(
            ResultsProviderParameters pam, CancellationToken cancellationToken);
        
        Task<WeightedDailyResult[]> GetNumericRespondentAverageResult(ResultsProviderParameters pam, ResponseFieldDescriptor field, CancellationToken cancellationToken);

        Task<Dictionary<string, EntityWeightedDailyResults[]>> GetMarketAverageWeightingsByBaseMeasureId(
            ResultsProviderParameters pam,
            CancellationToken cancellationToken);
        
        Task<Dictionary<string, UnweightedTotals>> GetMarketAverageUnweightedWeightingsByBaseMeasureId(
            ResultsProviderParameters pam,
            CancellationToken cancellationToken);
        
        Task<EntityWeightedDailyResults[]> CalculateWeightedForMeasure(ResultsProviderParameters pam,
            CancellationToken cancellationToken,
            Measure measureOverride = null,
            IGroupedQuotaCells quotaCellOverride = null);
        
        Task<UnweightedTotals> CalculateUnweightedForMeasure(ResultsProviderParameters pam,
            CancellationToken cancellationToken,
            Measure measureOverride = null,
            IGroupedQuotaCells quotaCellOverride = null);
        
        Task<EntityCategoryResults[]> WeightCategoryWithoutSignificance(UnweightedTotals unweighted,
            BreakdownCategory breakdownCategory,
            CancellationToken cancellationToken);

        Task<CategoryResults[]> MarketAverageForCategory(UnweightedTotals measureResults,
            UnweightedTotals relativeSizeResults,
            BreakdownCategory breakdownCategory,
            AverageType averageType,
            MainQuestionType questiontype,
            EntityMeanMap entityMeanMaps,
            CancellationToken cancellationToken);

        Task<IList<WeightedDailyResult>> CalculateMarketAverage(
            UnweightedTotals totalsForMeasure,
            UnweightedTotals marketAverageWeightingsForMeasure,
            Subset pamSubset,
            AverageType averageType,
            MainQuestionType questionType,
            EntityMeanMap entityMeanMaps,
            CancellationToken cancellationToken,
            IGroupedQuotaCells filteredCells = null);

        Task<int[]> CalculateRespondentIdsForMeasure(ResultsProviderParameters pam, CancellationToken cancellationToken);
    }
}