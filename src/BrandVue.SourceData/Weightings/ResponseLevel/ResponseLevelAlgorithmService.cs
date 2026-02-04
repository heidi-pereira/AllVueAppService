using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.LazyLoading;
using System.Threading;
using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Weightings;

namespace BrandVue.SourceData.Weightings.ResponseLevel
{
    public class ResponseLevelAlgorithmService
    {
        private const double DefaultWeightForMissingResponse = 1.0;
        private readonly IProductContext _productContext;
        private readonly ISubsetRepository _subsetRepository;
        private readonly IMeasureRepository _measureRepository;
        private readonly ISampleSizeProvider _sampleSizeProvider;
        private readonly IBaseExpressionGenerator _baseExpressionGenerator;
        private readonly IResponseWeightingRepository _responseWeightingRepository;
        private readonly IEntityRepository _entityRepository;
        private readonly IWeightingPlanRepository _weightingPlanRepository;
        private readonly IRespondentRepositorySource _respondentRepositorySource;

        public ResponseLevelAlgorithmService(IProductContext productContext, 
            ISubsetRepository subsetRepository,
            IMeasureRepository measureRepository,
            ISampleSizeProvider sampleSizeProvider,
            IBaseExpressionGenerator baseExpressionGenerator,
            IResponseWeightingRepository responseWeightingRepository,
            IEntityRepository entityRepository,
            IWeightingPlanRepository weightingPlanRepository,
            IRespondentRepositorySource respondentRepositorySource)
        {
            _productContext = productContext;
            _subsetRepository = subsetRepository;
            _measureRepository = measureRepository;
            _sampleSizeProvider = sampleSizeProvider;
            _baseExpressionGenerator = baseExpressionGenerator;
            _responseWeightingRepository = responseWeightingRepository;
            _entityRepository = entityRepository;
            _weightingPlanRepository = weightingPlanRepository;
            _respondentRepositorySource = respondentRepositorySource;
        }

        public enum ValidationMessageType
        {
            ExcelInvalidFile,
            ExcelMissingFile,
            ExcelMissingData,
            ExcelIgnoringRow,
            ExcelMissingSheet,
            SurveySegmentNotValid,
            DifferenceTooGreat,
        }

        public enum ExtraResponseReason
        {
            ID_Archived,
            ID_WeightTooSmall,
            ID_WeightTooLarge,
            ID_FoundInAlternativeWave,
            ID_NotFoundInSurvey,
            ID_NonExistent,
        }

        public enum InvalidResponseReason
        {
            ID_InvalidWeight,
            ID_ExtraResponsesInDatabaseForThisSurveyAndWave,
        }

        public record ExtraResponseWeight(int ResponseId, double? Weight, ExtraResponseReason Reason)
        {
            public string ReasonToAction
            {
                get
                {
                    switch (Reason)
                    {
                        case ExtraResponseReason.ID_Archived:
                            return "No action required";
                        case ExtraResponseReason.ID_FoundInAlternativeWave:
                        case ExtraResponseReason.ID_NonExistent:
                        case ExtraResponseReason.ID_NotFoundInSurvey:
                            return "No action required as response weight will be ignored";
                        case ExtraResponseReason.ID_WeightTooLarge:
                        case ExtraResponseReason.ID_WeightTooSmall:
                            return "Consider specifying weight in the range 0.2 to 5.0 (inclusive)";
                    }

                    return null;
                }
            }
            public string ReasonToDescription
            {
                get
                {
                    switch (Reason)
                    {
                        case ExtraResponseReason.ID_Archived:
                            return $"ResponseId {ResponseId} archived from survey";
                        case ExtraResponseReason.ID_FoundInAlternativeWave:
                            return $"ResponseId {ResponseId} not found in wave";
                        case ExtraResponseReason.ID_NonExistent:
                            return $"ResponseId {ResponseId} does not exist";
                        case ExtraResponseReason.ID_NotFoundInSurvey:
                            return $"ResponseId {ResponseId} not found in survey";
                        case ExtraResponseReason.ID_WeightTooLarge:
                        case ExtraResponseReason.ID_WeightTooSmall:
                            return "Weight is outside recommended thresholds";

                    }

                    return "Unknown";
                }
            }
        }

        public record InvalidResponseWeight(int ResponseId, double? Weight, InvalidResponseReason Reason)
        {
            public string ReasonToDescription
            {
                get
                {
                    switch (Reason)
                    {
                        case InvalidResponseReason.ID_ExtraResponsesInDatabaseForThisSurveyAndWave:
                            return $"ResponseId {ResponseId} missing from upload";

                        case InvalidResponseReason.ID_InvalidWeight:
                            return $"Weight {Weight} is invalid as it is less than zero";
                    }

                    return "Unknown reason";
                }
            }
        }
        public record ValidationMessage(ValidationMessageType MessageType, string Message);

        public class BasicExcelFileInformation
        {
            public string ExcelFileName { get; set; }
            public int NumberOfRows { get; set; }
            public long NumberOfBytes { get; set; }
            public DateTime DateTimeCreatedUtc { get; set; }
            public bool IsValid { get; set; }

            public BasicExcelFileInformation(string excelFileName)
            {
                ExcelFileName = excelFileName;
            }
        }

        public class ValidationStatistics : BasicExcelFileInformation, ISubsetIdProvider
        {
            public string SubsetId { get;}
            public int NumberOfValidRowsInExcel{ get; set; }
            public int NumberOfRowsInExcelIgnored { get; set; }
            public IList<ValidationMessage> Messages { get;}
            public IList<ExtraResponseWeight> ExtraResponsesInExcel { get;}
            public IList<ResponseWeight> ValidWeights { get; set; }
            public IList<InvalidResponseWeight> ErrorResponsesForThisSurveyAndWave { get;}
            public double? MaxWeight { get; set; }
            public double? MinWeight { get; set; }
            public int NumberOfResponsesMatched { get; set; }
            public int NumberOfResponsesInDatabaseForThisSurveyAndWave { get; set; }

            public ValidationStatistics(string subsetId, string excelFileName): base(excelFileName)
            {
                SubsetId = subsetId;
                Messages = new List<ValidationMessage>();
                ErrorResponsesForThisSurveyAndWave = new List<InvalidResponseWeight>();
                ExtraResponsesInExcel = new List<ExtraResponseWeight>();
            }
        }

        public record ResponseWeight(int ResponseId, double Weight);

        private IFilter CreateFilterForInstance(Subset subset, IList<WeightingFilterInstance> instances)
        {
            IFilter filter = new AlwaysIncludeFilter();

            var filters = instances.Where(instance => instance.FilterMetricName is not null && instance.FilterInstanceId is not null).Select(instance =>
            {
                return CreateFilter(subset, instance.FilterMetricName, instance.FilterInstanceId.Value);
            }).ToList();
            if (filters.Any())
            {
                if (filters.Count() == 1)
                {
                    return filters.First();
                }
                return new AndFilter(filters);
            }
            return filter;
        }

        private IFilter CreateFilter(Subset subset, string filterMetricName, int filterInstanceId)
        {
            var requestMeasure = _measureRepository.Get(filterMetricName);

            if (requestMeasure.IsVariableWithoutBaseExpression())
            {
                requestMeasure = _baseExpressionGenerator.GetMeasureWithOverriddenBaseExpression(requestMeasure,
                    new BaseExpressionDefinition
                    {
                        BaseType = BaseDefinitionType.SawThisQuestion,
                        BaseMeasureName = requestMeasure.Name,
                        BaseVariableId = null
                    });
            }
            var entityValues =
                new EntityValue(requestMeasure.EntityCombination.Single(), filterInstanceId);
            var entityValueCombination = new EntityValueCombination(entityValues);
            return new MetricFilter(subset, requestMeasure, entityValueCombination, new[] { filterInstanceId });
        }

        public async Task<int[]> GetWeights(string subsetId, List<WeightingFilterInstance> context,
            CancellationToken cancellationToken)
        {
            if (_subsetRepository.TryGet(subsetId, out var subset))
            {
                if (!subset.Disabled)
                {
                    return await Weights(context, subset, cancellationToken);
                }
            }
            return Array.Empty<int>();
        }

        private List<WeightingPlan> ConvertToWeightingPlan(List<WeightingFilterInstance> contexts)
        {
            if (contexts == null || contexts.Count == 0)
            {
                return null;
            }
            var myItem = contexts.FirstOrDefault();
            if (myItem == null)
            {
                return null;
            }
            var result = ConvertToWeightingPlan(contexts.Skip(1).ToList());
            var weightingTarget = new WeightingTarget(result, myItem.FilterInstanceId.GetValueOrDefault(),
                1.0m, null, null, null);

            var weightingPlan = new WeightingPlan(myItem.FilterMetricName, new List<WeightingTarget> { weightingTarget }, false, null);
            return new List<WeightingPlan>() { weightingPlan };

        }
        private async Task<int[]> Weights(List<WeightingFilterInstance> context, Subset subset,
            CancellationToken cancellationToken)
        {
            var filter = CreateFilterForInstance(subset, context);

            var weightingMeasures = new WeightingMetrics(_measureRepository, _entityRepository, subset, ConvertToWeightingPlan(context));

            return (await _sampleSizeProvider.GetRespondents(subset, filter, weightingMeasures, cancellationToken)).ToArray();
        }

        public async Task Validate(List<ResponseWeight> excelDefinedWeights, string subsetId,
            List<WeightingFilterInstance> context, ValidationStatistics statistics, CancellationToken cancellationToken)
        {
            statistics.IsValid = false;
            if (_subsetRepository.TryGet(subsetId, out var subset))
            {
                if (subset.Disabled)
                {
                    statistics.Messages.Add(new ValidationMessage(ValidationMessageType.SurveySegmentNotValid, $"Survey segment {subset.DisplayName} is disabled"));
                }
                else
                {
                    var allRespondentsInMemory = _respondentRepositorySource.GetForSubset(subset)
                        .Select(x => x.ProfileResponseEntity.Id)
                        .ToArray();
                    var filteredRespondents = await Weights(context, subset, cancellationToken);
                    var excelResponsesToWeight = excelDefinedWeights.ToDictionary(x => x.ResponseId, x => x.Weight);
                    ValidateMatching(filteredRespondents, excelResponsesToWeight, statistics, allRespondentsInMemory);
                    ValidateOnWeights(statistics, filteredRespondents, excelResponsesToWeight);
                }
            }
            else
            {
                statistics.Messages.Add(new ValidationMessage(ValidationMessageType.SurveySegmentNotValid, $"Survey segment {subsetId} does not exist"));
            }
        }

        private void ValidateMatching(int[] systemResponses,
            Dictionary<int, double> excelResponsesToWeight,
            ValidationStatistics statistics,
            int[] allRespondentsInMemory)
        {
            var excelDefinedResponses = excelResponsesToWeight.Select(x=> x.Key).ToArray();
            var mismatchingResponses = excelDefinedResponses.Except(systemResponses).ToArray();
            foreach (var mismatchingResponse in mismatchingResponses)
            {
                if (!systemResponses.Contains(mismatchingResponse))
                {
                    if(allRespondentsInMemory.Contains(mismatchingResponse))
                    {
                        statistics.ExtraResponsesInExcel.Add(new ExtraResponseWeight(mismatchingResponse, excelResponsesToWeight[mismatchingResponse], ExtraResponseReason.ID_FoundInAlternativeWave));
                    }
                    else
                    {
                        statistics.ExtraResponsesInExcel.Add(new ExtraResponseWeight(mismatchingResponse, excelResponsesToWeight[mismatchingResponse], ExtraResponseReason.ID_NotFoundInSurvey));
                    }
                }
            }

            statistics.NumberOfResponsesInDatabaseForThisSurveyAndWave = systemResponses.Length;
            statistics.NumberOfResponsesMatched = excelDefinedResponses.Intersect(systemResponses).Count();

            mismatchingResponses = systemResponses.Except(excelDefinedResponses).ToArray();
            foreach (var mismatchingResponse in mismatchingResponses)
            {
                if (!excelDefinedResponses.Contains(mismatchingResponse))
                {
                    statistics.ErrorResponsesForThisSurveyAndWave.Add(new InvalidResponseWeight(mismatchingResponse, DefaultWeightForMissingResponse, InvalidResponseReason.ID_ExtraResponsesInDatabaseForThisSurveyAndWave));
                }
            }

            statistics.ValidWeights = excelDefinedResponses.Where(x => !statistics.ErrorResponsesForThisSurveyAndWave.Any(y => y.ResponseId == x) && excelResponsesToWeight[x] >= 0.0)
                .Select(x => new ResponseWeight(x, excelResponsesToWeight[x]))
                .ToList();

            statistics.IsValid = !statistics.ErrorResponsesForThisSurveyAndWave.Any();
        }

        private const double MinWeightWarning = 0.2;
        private const double MaxWeightWarning = 5.0;
        private void ValidateOnWeights(ValidationStatistics statistics, int[] systemResponses, Dictionary<int, double> excelResponsesToWeight)
        {
            var totalWeight = 0.0;
            foreach (var responseId in systemResponses)
            {
                var weight = excelResponsesToWeight.ContainsKey(responseId) ? excelResponsesToWeight[responseId] : DefaultWeightForMissingResponse;
                
                if (weight < 0.0)
                {
                    statistics.ErrorResponsesForThisSurveyAndWave.Add(new InvalidResponseWeight(responseId, excelResponsesToWeight[responseId], InvalidResponseReason.ID_InvalidWeight));
                    statistics.IsValid = false;
                }
                else
                {
                    totalWeight += weight;
                    statistics.MinWeight = Math.Min(statistics.MinWeight.GetValueOrDefault(weight), weight);
                    statistics.MaxWeight = Math.Max(statistics.MaxWeight.GetValueOrDefault(weight), weight);
                    if (weight < MinWeightWarning)
                    {
                        
                        statistics.MinWeight = weight;
                        statistics.ExtraResponsesInExcel.Add(new ExtraResponseWeight(responseId,
                            excelResponsesToWeight[responseId], ExtraResponseReason.ID_WeightTooSmall));
                    }
                    else if (weight > MaxWeightWarning)
                    {

                        statistics.ExtraResponsesInExcel.Add(new ExtraResponseWeight(responseId,
                            excelResponsesToWeight[responseId], ExtraResponseReason.ID_WeightTooLarge));
                    }
                }
            }

            var difference = Math.Abs(totalWeight - systemResponses.Length);
            var maxAllowedPercentageDifference = 0.01;
            var maxAllowedDifference = systemResponses.Length * maxAllowedPercentageDifference;
            if (difference > maxAllowedDifference)
            {
                statistics.Messages.Add(new ValidationMessage(ValidationMessageType.DifferenceTooGreat,
                    $"Total weight {totalWeight} is more than {maxAllowedPercentageDifference * 100}% greater than number of responses {systemResponses.Length}"));
            }
        }

        public async Task PushIntoDatabase(List<ResponseWeight> weights, string subsetId,
            List<WeightingFilterInstance> weightingFilterInstances, CancellationToken cancellationToken)
        {
            if (_subsetRepository.TryGet(subsetId, out var subset))
            {
                if (subset.Disabled)
                {
                }
                else
                {
                    var respondentsFound = await Weights(weightingFilterInstances, subset, cancellationToken);

                    var dataToImport = respondentsFound.Select(id =>
                    {
                        return new ResponseWeightConfiguration() { RespondentId = id, Weight = Convert.ToDecimal(weights.SingleOrDefault(x => x.ResponseId == id)?.Weight ?? DefaultWeightForMissingResponse) };
                    });

                    var rootWeighting = weightingFilterInstances.Count == 0;

                    if (rootWeighting)
                    {
                        _responseWeightingRepository.CreateResponseWeightsForRoot(subsetId, dataToImport.ToList());
                    }
                    else
                    {
                        var plans = _weightingPlanRepository.GetWeightingPlansForSubset(_productContext.ShortCode, _productContext.SubProductId, subsetId);
                        var filterInstances  = weightingFilterInstances.Select(x =>
                            new TargetInstance(x.FilterMetricName, x.FilterInstanceId)).ToList();

                        var plansWithMissingTargetsAdded = AddMissingTargetsToPlans(subsetId, 0, filterInstances, plans);

                        _responseWeightingRepository.CreateResponseWeights(subsetId, plansWithMissingTargetsAdded, filterInstances, dataToImport);
                    }
                }
            }
            else
            {
            }
        }

        private IReadOnlyCollection<WeightingPlanConfiguration> AddMissingTargetsToPlans(string subsetId, int depth,
            IList<TargetInstance> pathOfTargetInstances,
            IReadOnlyCollection<WeightingPlanConfiguration> root)
        {
            IReadOnlyCollection<WeightingPlanConfiguration> plans = root;
            var plansContainNewTargets = false;
            while (depth < pathOfTargetInstances.Count)
            {
                var instance = pathOfTargetInstances[depth];
                var target = plans.Where(p => p.VariableIdentifier == instance.FilterMetricName)
                    .SelectMany(p => p.ChildTargets)
                    .FirstOrDefault(t => t.EntityInstanceId == instance.FilterInstanceId);

                if (target == null)
                {
                    try
                    {
                        var parentPlan = plans.First(p => p.VariableIdentifier == instance.FilterMetricName);
                        
                        target = new WeightingTargetConfiguration()
                        {
                            Id = 0,
                            EntityInstanceId = (int)instance.FilterInstanceId,
                            Target = null,
                            TargetPopulation = null,
                            ParentWeightingPlanId = parentPlan.Id,
                            ParentWeightingPlan = parentPlan,
                            ChildPlans = null,
                            ProductShortCode = parentPlan.ProductShortCode,
                            SubProductId = parentPlan.SubProductId,
                            SubsetId = parentPlan.SubsetId
                        };
                        parentPlan.ChildTargets.Add(target);
                        plansContainNewTargets = true;
                    }
                    catch (InvalidOperationException e)
                    {
                        throw new InvalidOperationException($"Could not create a target for FilterMetricName: {instance.FilterMetricName}, instance id: {instance.FilterInstanceId} in subset {subsetId}", e);
                    } 
                }
                depth++;
                plans = target.ChildPlans;
            }

            if (plansContainNewTargets)
            {
                _weightingPlanRepository.UpdateWeightingPlanForSubset(_productContext.ShortCode, _productContext.SubProductId, subsetId, root);
            }

            return root;
        }
    }
}
