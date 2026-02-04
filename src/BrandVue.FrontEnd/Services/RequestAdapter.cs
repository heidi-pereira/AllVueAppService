using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.EntityFramework.MetaData.Weightings;
using BrandVue.Middleware;
using BrandVue.Models;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Weightings;
using Microsoft.Extensions.Logging;
using Vue.Common.Auth.Permissions;

namespace BrandVue.Services
{
    public class RequestAdapter : IRequestAdapter
    {
        private readonly ISubsetRepository _subsetRepository;
        private readonly IAverageDescriptorRepository _averageDescriptorRepository;
        private readonly IMeasureRepository _measureRepository;
        private readonly IEntityRepository _entityRepository;
        private readonly IDemographicFilterToQuotaCellMapper _demographicFilterToQuotaCellMapper;
        private readonly IResponseEntityTypeRepository _responseEntityTypeRepository;
        private readonly IWeightingPlanRepository _weightingPlanRepository;
        private readonly IFilterRepository _filter;
        private readonly IProductContext _productContext;
        private readonly IBaseExpressionGenerator _baseExpressionGenerator;
        private readonly RequestScope _requestScope;
        private readonly IQuestionTypeLookupRepository _questionTypeLookupRepository;
        private readonly IUserDataPermissionsService _userDataPermissionsService;

        public RequestAdapter(
            ISubsetRepository subsetRepository,
            IAverageDescriptorRepository averageDescriptorRepository,
            IMeasureRepository measureRepository,
            IEntityRepository entityRepository,
            IDemographicFilterToQuotaCellMapper demographicFilterToQuotaCellMapper,
            IResponseEntityTypeRepository responseEntityTypeRepository,
            IWeightingPlanRepository weightingPlanRepository,
            IFilterRepository filter,
            IProductContext productContext,
            IBaseExpressionGenerator baseExpressionGenerator,
            RequestScope requestScope,
            IQuestionTypeLookupRepository questionTypeLookupRepository,
            IUserDataPermissionsService userDataPermissionsService)
        {
            _subsetRepository = subsetRepository;
            _averageDescriptorRepository = averageDescriptorRepository;
            _measureRepository = measureRepository;
            _entityRepository = entityRepository;
            _demographicFilterToQuotaCellMapper = demographicFilterToQuotaCellMapper;
            _responseEntityTypeRepository = responseEntityTypeRepository;
            _weightingPlanRepository = weightingPlanRepository;
            _filter = filter;
            _productContext = productContext;
            _baseExpressionGenerator = baseExpressionGenerator;
            _requestScope = requestScope;
            _questionTypeLookupRepository = questionTypeLookupRepository;
            _userDataPermissionsService = userDataPermissionsService;
        }

        public ResultsProviderParameters CreateParametersForCalculation(CuratedResultsModel model, CompositeFilterModel filterModel,
           bool onlyUseFocusInstance = false, bool alwaysIncludeActiveBrand = true)
        {
            var subset = _subsetRepository.Get(model.SubsetId);
            var entityInstanceIds = onlyUseFocusInstance ? new [] { model.ActiveBrandId } :  model.EntityInstanceIds;

            if (!model.MeasureName.Any())
            {
                throw new ArgumentException("MeasureName in request has no valid metric", nameof(model));
            }


            if (model.BaseExpressionOverride != null && model.MeasureName.Length > 1)
            {
                throw new ArgumentException("Base expression override can't be used with multi-metrics");
            }

            var measures = model.MeasureName.Select(measureName => _measureRepository.Get(measureName))
                .Select(measure => _baseExpressionGenerator.GetMeasureWithOverriddenBaseExpression(measure, model.BaseExpressionOverride))
                .ToArray();
            var primaryMeasure = measures[0];


            if (primaryMeasure.EntityCombination.Count() > 1)
            {
                throw new ArgumentException(
                    "Metric with multiple entity combinations can not be used with multimetrics.");
            }

            var requestedEntityType = primaryMeasure.EntityCombination.SingleOrDefault() ?? EntityType.ProfileType;
            var parameters = CreateParametersForCalculationInternal(
                model.SubsetId,
                primaryMeasure,
                new TargetInstances(requestedEntityType, GetOrderedEntityInstancesFromIds(entityInstanceIds, model.ActiveBrandId, alwaysIncludeActiveBrand, requestedEntityType, subset)),
                filterModel,
                model.DemographicFilter,
                model.Period,
                measures,
                Array.Empty<TargetInstances>()
            );

            parameters.DoMeasuresIncludeMarketMetric = measures.Any(m => !m.EntityCombination.Any());

            bool isBrand = parameters.PrimaryMeasure.EntityCombination.Count() == 1 && parameters.PrimaryMeasure.EntityCombination.Single().IsBrand;
            parameters.CompetitorsContainsActiveBrand = entityInstanceIds.Contains(model.ActiveBrandId);
            parameters.SampleSizeEntityInstanceId = isBrand ? model.ActiveBrandId : null;
            parameters.LowSampleEntityInstanceId = isBrand ? model.ActiveBrandId : null;
            parameters.FocusEntityInstanceId = isBrand ? parameters.SampleSizeEntityInstanceId : null;

            var activeBrandInBrands = parameters.EntityInstances.Select((brand, index) => new { id = brand.Id, index }).SingleOrDefault(r => r.id == model.ActiveBrandId);
            var activeBrandIndex = activeBrandInBrands?.index ?? 0;

            parameters.MultiMetricEntityInstanceIndex = isBrand? activeBrandIndex : 0;
            parameters.FunnelEntityInstanceIndex = isBrand? activeBrandIndex : 0;
            parameters.ScorecardEntityInstanceIndex = isBrand ? activeBrandIndex : 0;
            parameters.IncludeSignificance = model.IncludeSignificance;
            parameters.SigConfidenceLevel = model.SigConfidenceLevel;
            return parameters;
        }


        public ResultsProviderParameters CreateParametersForCalculation(CuratedResultsModel model,
            bool onlyUseFocusInstance = false, MeasureFilterRequestModel additionalFilterRequestModel = null, bool alwaysIncludeActiveBrand = true, CrossMeasure[] crossMeasures = null)
        {
            var filterModel = additionalFilterRequestModel == null ? model.FilterModel : new CompositeFilterModel(FilterOperator.And, additionalFilterRequestModel.Yield(), model.FilterModel.Yield());
            var result = CreateParametersForCalculation(model, filterModel, onlyUseFocusInstance, alwaysIncludeActiveBrand);
            SetBreaks(result, crossMeasures, false);
            return result;
        }

        public ResultsProviderParameters CreateParametersForCalculationWithAdditionalFilter(MultiEntityRequestModel model, MeasureFilterRequestModel additionalFilterRequestModel = null)
        {
            var filterModel = additionalFilterRequestModel == null ? model.FilterModel : new CompositeFilterModel(FilterOperator.And, additionalFilterRequestModel.Yield(), model.FilterModel.Yield());
            return CreateParametersForCalculation(model, filterModel);
        }

        public ResultsProviderParameters CreateParametersForCalculation(MultiEntityRequestModel model,
            CompositeFilterModel filterModel = null, CrossMeasure[] crossMeasureBreaks = null)
        {
            var subset = _subsetRepository.Get(model.SubsetId);
            var filterInstances = Array.Empty<TargetInstances>();
            if (model.FilterBy is {})
            {
                filterInstances = model.FilterBy.Select(instanceRequest =>
                {
                    var filterByEntityType = _responseEntityTypeRepository.Get(instanceRequest.Type);
                    return new TargetInstances(filterByEntityType, GetOrderedEntityInstancesFromIds(filterByEntityType, instanceRequest.EntityInstanceIds, subset));
                }).ToArray();
            }

            var measure = _measureRepository.Get(model.MeasureName);
            var measures = new List<Measure>();
            //
            // If measure has no entities then the front end builds up the wrong model
            //
            var requestedEntityType = measure.EntityCombination.Any() ? model.DataRequest.Type : EntityType.ProfileType.Identifier;
            var dataRequestEntityType = _responseEntityTypeRepository.Get(requestedEntityType);

            if (model.BaseExpressionOverrides.Any())
            {
                measures.AddRange(model.BaseExpressionOverrides.Select(baseExpressionOverride => _baseExpressionGenerator.GetMeasureWithOverriddenBaseExpression(measure, baseExpressionOverride)));
            }
            else
            {
                measures.Add(measure);
            }

            var parameters = CreateParametersForCalculationInternal(
                model.SubsetId,
                measure,
                new TargetInstances(dataRequestEntityType, GetOrderedEntityInstancesFromIds(dataRequestEntityType, model.DataRequest.EntityInstanceIds, subset)),
                filterModel ?? model.FilterModel,
                model.DemographicFilter,
                model.Period,
                measures.ToArray(),
                filterInstances,
                model.FocusEntityInstanceId
            );
            SetBreaks(parameters, crossMeasureBreaks, false);

            var isSingleBrandEntity = parameters.PrimaryMeasure.EntityCombination.Count() == 1 &&
                                 parameters.PrimaryMeasure.EntityCombination.Single().IsBrand;

            if (isSingleBrandEntity)
            {
                if (model.FocusEntityInstanceId != null)
                {
                    parameters.CompetitorsContainsActiveBrand = model.DataRequest.EntityInstanceIds.ToList().Contains((int)model.FocusEntityInstanceId);
                }
            }

            parameters.SampleSizeEntityInstanceId = model.FocusEntityInstanceId;
            parameters.LowSampleEntityInstanceId = model.FocusEntityInstanceId;

            parameters.IncludeSignificance = model.IncludeSignificance;
            parameters.SigConfidenceLevel = model.SigConfidenceLevel;

            return parameters;
        }

        public IReadOnlyCollection<ResultsProviderParameters> CreateParametersForCalculation(StackedMultiEntityRequestModel model)
        {
            var subset = _subsetRepository.Get(model.SubsetId);
            var filterByEntityType = _responseEntityTypeRepository.Get(model.FilterBy.Type);
            var filterByInstances = GetOrderedEntityInstancesFromIds(filterByEntityType, model.FilterBy.EntityInstanceIds, subset);

            var splitByEntityType = _responseEntityTypeRepository.Get(model.SplitBy.Type);
            var splitByTargetInstances = new TargetInstances(splitByEntityType, GetOrderedEntityInstancesFromIds(splitByEntityType, model.SplitBy.EntityInstanceIds, subset));

            var measure = _measureRepository.Get(model.MeasureName);
            measure = _baseExpressionGenerator.GetMeasureWithOverriddenBaseExpression(measure, model.BaseExpressionOverride);

            return filterByInstances.Select(filterInstance => CreateParametersForCalculationInternal(
                    model.SubsetId,
                    measure,
                    splitByTargetInstances,
                    model.FilterModel,
                    model.DemographicFilter,
                    model.Period,
                    new[] { measure },
                    new[] { new TargetInstances(filterByEntityType, new[] { filterInstance }) }
                )
            ).ToArray();
        }

        public ResultsProviderParameters CreateParametersForCalculation(CrosstabRequestModel model,
            Measure measure, TargetInstances requestedInstances, CompositeFilterModel filterModel, TargetInstances[] filterInstances, bool legacyCalculation)
        {
            var result = CreateParametersForCalculationInternal(
                model.SubsetId,
                measure,
                requestedInstances,
                filterModel,
                model.DemographicFilter,
                model.Period,
                new[] { measure },
                filterInstances
            );
            SetBreaks(result, model.CrossMeasures, legacyCalculation);
            result.IncludeSignificance = model.Options?.CalculateSignificance ?? false;
            result.SigConfidenceLevel = model.Options?.SigConfidenceLevel ?? SigConfidenceLevel.NinetyFive;
            return result;
        }

        public ResultsProviderParameters CreateParametersForCalculation(TemporaryVariableRequestModel model,
            Measure measure, TargetInstances requestedInstances, CompositeFilterModel filterModel, TargetInstances[] filterInstances, Break[] breaks)
        {
            var result = CreateParametersForCalculationInternal(
                model.SubsetId,
                measure,
                requestedInstances,
                filterModel,
                model.DemographicFilter,
                model.Period,
                [measure],
                filterInstances
            );
            result.Breaks = breaks;
            return result;
        }

        private void SetBreaks(ResultsProviderParameters result, CrossMeasure[] crossMeasures,
            bool legacyCalculation)
        {
            result.Breaks = legacyCalculation || crossMeasures is null ? Array.Empty<Break>() : CreateBreaks(crossMeasures, result.Subset.Id);
        }

        public Break[] CreateBreaks(CrossMeasure[] crossMeasures, string subsetId)
        {
            var subset = _subsetRepository.Get(subsetId);
            var breaks = new Break[crossMeasures.Length];
            var startInstanceIndex = 0;
            for (int breakIndex = 0; breakIndex < crossMeasures.Length; breakIndex++)
            {
                var crossMeasure = crossMeasures[breakIndex];
                var @break = CreateBreak(crossMeasure, startInstanceIndex);
                breaks[breakIndex] = @break;
                startInstanceIndex += @break.Instances.Length;
            }
            return breaks;

            Break CreateBreak(CrossMeasure cm, int instanceIndexOffset)
            {
                var measure = _measureRepository.Get(cm.MeasureName);
                if (measure.GenerationType != AutoGenerationType.CreatedFromField && !measure.HasBaseExpression)
                {
                    // PERF: Don't bother passing "SawThisQuestion", since for breaks we iterate over their answers so won't do anything if they have no answers
                    measure = _baseExpressionGenerator.GetMeasureWithOverriddenBaseExpression(measure, new BaseExpressionDefinition
                    {
                        BaseType = BaseDefinitionType.AllRespondents,
                        BaseMeasureName = measure.Name,
                        BaseVariableId = null
                    });
                }

                int[] instances = GetBreakInstances(measure, cm);
                var baseInstances = measure.BaseExpression.UserEntityCombination.Any() ?
                    _entityRepository.GetInstancesOf(measure.BaseExpression.UserEntityCombination.Single().Identifier, subset).Select(x => x.Id).ToArray() :
                    Array.Empty<int>();

                // Legacy path used when PrimaryVariable not available
                var childBreak = CreateBreaks(cm.ChildMeasures, subsetId);
                return new Break(measure.PrimaryVariable, measure.BaseExpression, instances, baseInstances, childBreak, instanceIndexOffset);
            }

            int[] GetBreakInstances(Measure measure, CrossMeasure cm)
            {
                //
                // Code needs to be refactored as per https://app.shortcut.com/mig-global/story/81822/refactor-code-requestadapter-getbreakinstances-crosstabfiltermodelfactory-getallfiltersformeasure
                //
                var isBasedOnSingleChoiceOrVariable = _questionTypeLookupRepository.GetForSubset(subset)
                    .TryGetValue(measure.Name, out var questionType) && (questionType == MainQuestionType.SingleChoice || questionType == MainQuestionType.CustomVariable);

                //TODO: Dedupe OrderBy with one in CrosstabFilterModelFactory.GetSingleEntityFilters so they don't have to happen to line up
                return measure switch
                {
                    var m when m.EntityCombination.Count() > 1 => throw new ArgumentException("CrossMeasures can't be multi-entity"),
                    var m when m.EntityCombination.Any() && !isBasedOnSingleChoiceOrVariable => GetMultipleChoiceInstances(measure, cm),
                    var m when CanUseFilterValueMappingInstances(m, cm) => GetMappedInstances(measure, cm),
                    var m when m.EntityCombination.Any() => GetEntityInstances(measure, cm),
                    _ => throw new ArgumentException("CrossMeasures without an entity type must have a filter mapping")
                };
            }

            bool CanUseFilterValueMappingInstances(Measure measure, CrossMeasure cm)
            {
                return !string.IsNullOrWhiteSpace(measure.FilterValueMapping) && (
                    !cm.FilterInstances.Any() || cm.FilterInstances.Any(i => !string.IsNullOrWhiteSpace(i.FilterValueMappingName))
                );
            }

            int[] GetMultipleChoiceInstances(Measure measure, CrossMeasure cm)
            {
                if (cm.MultipleChoiceByValue && !string.IsNullOrWhiteSpace(measure.FilterValueMapping) &&
                    !measure.FilterValueMapping.StartsWith("Range", StringComparison.InvariantCultureIgnoreCase))
                {
                    return GetMappedInstances(measure, cm);
                }
                else
                {
                    return GetEntityInstances(measure, cm);
                }
            }

            int[] GetMappedInstances(Measure measure, CrossMeasure cm)
            {
                if (measure.FilterValueMapping.StartsWith("Range", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new ArgumentException("Can't create a crosstab break from a \"Range\" filter mapping");
                }

                var mappings = FilterValueMappingVariableParser.FilterMeasures(measure.FilterValueMapping);
                if (cm.FilterInstances.Any())
                {
                    var includedNames = cm.FilterInstances.Select(f => f.FilterValueMappingName).ToHashSet();
                    mappings = mappings.Where(mapping => includedNames.Contains(mapping.Name)).ToArray();
                }
                //more than 1 value uses legacy calculation
                return mappings.Select(m => m.Values.Single()).OrderBy(id => id).ToArray();
            }

            int[] GetEntityInstances(Measure measure, CrossMeasure cm)
            {
                if (cm.FilterInstances.Any())
                {
                    return cm.FilterInstances.Select(f => f.InstanceId).OrderBy(id => id).ToArray();
                }
                return _entityRepository.GetInstancesOf(measure.PrimaryVariable.UserEntityCombination.Single().Identifier, subset)
                        .Select(x => x.Id).OrderBy(id => id).ToArray();
            }
        }

        public IReadOnlyCollection<ResultsProviderParameters> CreateCalculationParametersPerPeriod(CuratedResultsModel model)
        {
            return model.Period.ComparisonDates.Select(p =>
            {
                var newModel = model with {Period = new Period {Average = model.Period.Average, ComparisonDates = new[] {p}}};
                return CreateParametersForCalculation(newModel);
            }).ToArray();
        }

        public IReadOnlyCollection<ResultsProviderParameters> CreateCalculationParametersPerPeriod(MultiEntityRequestModel model)
        {
            return model.Period.ComparisonDates.Select(p =>
            {
                var newModel = model with {Period = new Period {Average = model.Period.Average, ComparisonDates = new[] {p}}};
                return CreateParametersForCalculation(newModel);
            }).ToArray();
        }

        private ResultsProviderParameters CreateParametersForCalculationInternal(
            string subsetId,
            Measure measure,
            TargetInstances requestedInstances,
            CompositeFilterModel filterModel,
            DemographicFilter demographicFilter,
            Period period,
            Measure[] measures,
            TargetInstances[] filterInstances,
            int? focusBrandId = null
        )
        {
            if (!_subsetRepository.TryGet(subsetId, out var subset))
            {
                throw new ArgumentOutOfRangeException(nameof(subsetId), subsetId, $@"Invalid data subset: '{subsetId}'.");
            }

            var parameters = new ResultsProviderParameters
            {
                Subset = subset,
                PrimaryMeasure = measure,
                RequestedInstances = requestedInstances,
                FilterModel = UpdateCompositeFilterModelForCurrentUser(filterModel),
                Average = _averageDescriptorRepository.Get(period.Average, _requestScope.Organization),
                CalculationPeriod = new CalculationPeriod { Periods = period.ComparisonDates },
                Measures = measures,
                FilterInstances = filterInstances,
                QuestionType = GetQuestionType(subsetId, measure.Name),
                EntityMeanMaps = measure.EntityInstanceIdMeanCalculationValueMapping
            };

            parameters.QuotaCells = _demographicFilterToQuotaCellMapper
                .MapQuotaCellsFor(
                    parameters.Subset,
                    demographicFilter.Patch(_filter),
                    parameters.Average);

            parameters.EntityRepository = _entityRepository;

            if (focusBrandId.HasValue)
            {
                parameters.FocusEntityInstanceId = focusBrandId.Value;
            }

            return parameters;
        }

        private CompositeFilterModel UpdateCompositeFilterModelForCurrentUser(CompositeFilterModel originalFilterModel)
        {
            var dataPermission = _userDataPermissionsService.GetDataPermission();

            if (dataPermission == null || !dataPermission.Filters.Any())
            {
                return originalFilterModel;
            }

            return UpdateFilterModelForDataPermissionFilters(originalFilterModel, dataPermission.Filters);
        }

        private CompositeFilterModel UpdateFilterModelForDataPermissionFilters(CompositeFilterModel originalFilterModel, IList<DataPermissionFilterDto> dataPermissionFilters)
        {
            var variableConfigurationIds = dataPermissionFilters.Select(filter => filter.VariableConfigurationId).ToList();
            var validMeasuresToFilterBy = _measureRepository.GetMeasuresByVariableConfigurationIds(variableConfigurationIds).ToList();

            if (!validMeasuresToFilterBy.Any())
            {
                return originalFilterModel;
            }

            return new CompositeFilterModel(FilterOperator.And, CreateFiltersForUserPermission(dataPermissionFilters, validMeasuresToFilterBy), originalFilterModel.Yield());
        }

        private IEnumerable<MeasureFilterRequestModel> CreateFiltersForUserPermission(IList<DataPermissionFilterDto> dataPermissionFilters, List<Measure> measuresToFilterBy)
        {
            var filterOptionsLookup = dataPermissionFilters.ToDictionary(filter => filter.VariableConfigurationId,
                filter => filter.EntityInstanceId);

            return measuresToFilterBy.Select(measure => CreateMeasureFilterRequestModelFromEntityInstances(
                    measure.Name, 
                    measure.EntityCombination.First().Identifier, 
                    filterOptionsLookup[measure.VariableConfigurationId!.Value].ToArray()
                    )
            );
        }

        private MeasureFilterRequestModel CreateMeasureFilterRequestModelFromEntityInstances(string measureName, string entityInstanceIdentifier, int[] instanceIds)
        {
            var entityInstance = new Dictionary<string, int[]> {
            {
                entityInstanceIdentifier,
                instanceIds
            }};
            return new MeasureFilterRequestModel(measureName, entityInstance, false, false, instanceIds);
        }

        private MainQuestionType GetQuestionType(string subsetId, string name)
        {
            var lookup = _questionTypeLookupRepository.GetForSubset(_subsetRepository.Get(subsetId));
            return lookup.TryGetValue(name, out var questionType) ? questionType : MainQuestionType.Unknown;
        }

        private IEnumerable<EntityInstance> GetOrderedEntityInstancesFromIds(int[] instanceIds, int activeBrandId,
            bool alwaysIncludeActiveBrand, EntityType measureType, Subset subset)
        {
            if (measureType.IsProfile)
            {
                return Enumerable.Empty<EntityInstance>();
            }

            if (alwaysIncludeActiveBrand && measureType.IsBrand && !instanceIds.Contains(activeBrandId))
            {
                var allBrands = new List<int>(instanceIds) {activeBrandId};
                instanceIds = allBrands.ToArray();
            }

            return GetOrderedEntityInstancesFromIds(measureType, instanceIds, subset);
        }

        private IEnumerable<EntityInstance> GetOrderedEntityInstancesFromIds(EntityType entityType,
            int[] entityInstanceIds, Subset subset)
        {
            return _entityRepository
                .GetInstances(entityType.Identifier, entityInstanceIds, subset)
                .OrderBy(i => i.Id); // This is crucial since we expect the results to come out in the same order we put them in, but in actuality they come out in brand id order.
        }

        public IGroupedQuotaCells GetFilterOptimizedQuotaCells(Subset subset, IGroupedQuotaCells originalCells)
        {
            if (GetWeightingPlanOrNull(subset.Id) is not { } weightingPlanConfigurations) return originalCells;
            var weightingPlans = weightingPlanConfigurations.ToAppModel();
            var weightingMeasures = new WeightingMetrics(_measureRepository, _entityRepository, subset, weightingPlans);
            return EnforcedFilteredGroupedQuotaCells.Create(originalCells, weightingMeasures.IntersectedMeasureDependencies);
        }

        public ResultsProviderParameters CreateParametersForCalculation(string measureName,
            string subsetId,
            Period period,
            EntityInstanceRequest dataRequest,
            bool includeSignificance,
            SigConfidenceLevel sigConfidenceLevel,
            EntityInstanceRequest[] filterBy = null)
        {
            var multiRequestModel = new MultiEntityRequestModel(measureName,
                subsetId,
                period,
                dataRequest,
                filterBy,
                new DemographicFilter(_filter),
                new CompositeFilterModel(),
                Array.Empty<MeasureFilterRequestModel>(),
                Array.Empty<BaseExpressionDefinition>(),
                includeSignificance,
                sigConfidenceLevel);

            return CreateParametersForCalculation(multiRequestModel);
        }

        private IReadOnlyCollection<WeightingPlanConfiguration> GetWeightingPlanOrNull(string subsetId)
        {
            var weightingPlanConfigurations = _weightingPlanRepository.GetLoaderWeightingPlansForSubset(_productContext.ShortCode, _productContext.SubProductId, subsetId);
            if (weightingPlanConfigurations == null || !weightingPlanConfigurations.Any())
            {
                return null;
            }
            return weightingPlanConfigurations;
        }
    }
}
