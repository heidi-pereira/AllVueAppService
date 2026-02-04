using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Subsets;

namespace BrandVue.Services
{
    /// <summary>
    /// Exposes a service to convert request models to <see cref="ResultsProviderParameters"/> for use with <see cref="ConvenientCalculator"/>
    /// </summary>
    public interface IRequestAdapter
    {
        ResultsProviderParameters CreateParametersForCalculation(CuratedResultsModel model,
            bool onlyUseFocusInstance = false,
            MeasureFilterRequestModel additionalFilterRequestModel = null,
            bool alwaysIncludeActiveBrand = true,
            CrossMeasure[] crossMeasures = null);
        
        ResultsProviderParameters CreateParametersForCalculation(CuratedResultsModel model, CompositeFilterModel filterModel,
           bool onlyUseFocusInstance = false, bool alwaysIncludeActiveBrand = true);

        ResultsProviderParameters CreateParametersForCalculationWithAdditionalFilter(MultiEntityRequestModel model,
            MeasureFilterRequestModel additionalFilterRequestModel = null);

        ResultsProviderParameters CreateParametersForCalculation(MultiEntityRequestModel model,
            CompositeFilterModel filterModel = null, CrossMeasure[] crossMeasureBreaks = null);
        
        ResultsProviderParameters CreateParametersForCalculation(string measureName,
            string subsetId,
            Period period,
            EntityInstanceRequest dataRequest,
            bool includeSignificance,
            SigConfidenceLevel sigConfidenceLevel,
            EntityInstanceRequest[] filterBy = null);

        ResultsProviderParameters CreateParametersForCalculation(CrosstabRequestModel model,
            Measure measure, TargetInstances requestedInstances,
            CompositeFilterModel filterModel, TargetInstances[] filterInstances, bool legacyCalculation);

        ResultsProviderParameters CreateParametersForCalculation(TemporaryVariableRequestModel model,
            Measure measure, TargetInstances requestedInstances,
            CompositeFilterModel filterModel, TargetInstances[] filterInstances, Break[] breaks);

        IReadOnlyCollection<ResultsProviderParameters> CreateCalculationParametersPerPeriod(CuratedResultsModel model);
        IReadOnlyCollection<ResultsProviderParameters> CreateCalculationParametersPerPeriod(MultiEntityRequestModel model);
        IReadOnlyCollection<ResultsProviderParameters> CreateParametersForCalculation(StackedMultiEntityRequestModel model);
        IGroupedQuotaCells GetFilterOptimizedQuotaCells(Subset subset, IGroupedQuotaCells exampleParametersQuotaCells);

        Break[] CreateBreaks(CrossMeasure[] crossMeasures, string subsetId);
    }
}
