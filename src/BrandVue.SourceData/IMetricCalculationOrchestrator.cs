using System.Threading;
using System.Threading.Tasks;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Models;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData
{
    public interface IMetricCalculationOrchestrator
    {
        /// <summary>
        /// Main path for calculating weighted results.
        /// 
        /// See <see cref="CalculateUnweightedTotals"/> and <see cref="CalculateWeightedFromUnweighted"/> for details.
        /// </summary>
        Task<EntityWeightedDailyResults[]> Calculate(FilteredMetric filteredMetric,
            CalculationPeriod calculationPeriod,
            AverageDescriptor average,
            TargetInstances requestedInstances,
            IGroupedQuotaCells quotaCells,
            bool calculateSignificance,
            CancellationToken cancellationToken);

        /// <summary>
        /// Totalise:
        /// 	* Calculates and sums metric values (and count) for respondents matching base/filter.
        /// 	* Returns UnweightedTotals: i.e. per entity, a time series of per cell totals
        /// 	* The metric value is the variable value, transformed by the calctype (e.g. average just uses the value, percentages map to 1 or 0).
        /// </summary>
        /// <returns>A result per requested instance in ascending instance id order</returns>
        public Task<UnweightedTotals> CalculateUnweightedTotals(FilteredMetric filteredMetric,
            CalculationPeriod calculationPeriod,
            AverageDescriptor average,
            TargetInstances requestedInstances,
            IGroupedQuotaCells quotaCells,
            CancellationToken cancellationToken,
            EntityWeightedDailyResults[] weightedAverages = null);

        /// <summary>
        /// Weight totals:
        /// 	* Calculates the appropriate weighting multipliers
        /// 	* Weighted sum of all value totals and count totals (x1 * w1 + x2 * w2 + ...)
        /// 	* Returns per entity, a time series of weighted totals
        /// Finalise results:
        /// 	* Aggregate: Sums weighted totals for requested average
        /// 		* e.g. if weighting monthly, but requesting quarterly results, need to sum 3 months of totals into each point
        /// 		* e.g. if weighting daily requesting daily data points over 3 weeks, need to do a sliding window sum of 3 weeks into each total
        /// 	* Divides the total value by the total count
        /// 	* Applies any scale factor attached to the metric
        /// 	* Removes any data that falls outside the defined valid data range for a metric
        /// </summary>
        Task<EntityWeightedDailyResults[]> CalculateWeightedFromUnweighted(UnweightedTotals unweighted,
            bool calculateSignificance, CancellationToken cancellationToken, IGroupedQuotaCells filteredCells = null);

        /// <summary>
        /// Averages across entities
        /// Separate method to allow creating several averages for different groups of entities for the same result set without recalculating
        /// Supports: ResultMean (most common), EntityIdMean and Median
        /// Can also calculate a weighted average taking into account brand size and demographic and accounts for any sampling bias between the base field and this field (but not transitively).
        /// </summary>
        /// <param name="measureResults"></param>
        /// <param name="subset"></param>
        /// <param name="minimumSamplePerPoint"></param>
        /// <param name="relativeSizes">If not null, calculates the average weighted per customer, rather than a simple average over instances.</param>
        IList<WeightedDailyResult> CalculateMarketAverage(EntityWeightedDailyResults[] measureResults,
            Subset subset,
            ushort minimumSamplePerPoint,
            AverageType averageType,
            MainQuestionType questionType,
            EntityMeanMap entityMeanMap,
            EntityWeightedDailyResults[] relativeSizes = null);

        /// <summary>
        /// TODO If you know what this is for, fill in this doc
        /// It gets the weighted results, then does some extra reweighting grouped by responseid
        /// Probably better to just create a variable to count the mentions for a given respondent?
        /// </summary>
        Task<WeightedDailyResult> CalculateAverageMentions(FilteredMetric filteredMetric,
            CalculationPeriod calculationPeriod,
            AverageDescriptor average,
            TargetInstances requestedInstances,
            IGroupedQuotaCells quotaCells,
            CancellationToken cancellationToken);

        /// <remarks>
        /// Best guess:
        /// The standard calculate with metric type avg is (afaik) the same as "Mean" or "ResultMean". Here, several other options are available.
        /// Doing things like taking the average of the entityid may be why it has its own code path.
        /// I don't think this necessarily works for all AverageDescriptors, the code seems to be a cutdown/inlined copy of <see cref="InMemoryTotalisationOrchestrator.TotaliseAsync"/> and the callstack below it
        /// Probably better would be to create an ephemeral variable wrapper to reshape the data and then use the standard method.
        /// </remarks>
        Task<WeightedDailyResult[]> CalculateNumericResponseAverage(FilteredMetric filteredMetric,
            CalculationPeriod calculationPeriod,
            AverageDescriptor average,
            TargetInstances requestedInstances,
            IGroupedQuotaCells quotaCells,
            AverageType averageType,
            ResponseFieldDescriptor field,
            CancellationToken cancellationToken);
    }
}