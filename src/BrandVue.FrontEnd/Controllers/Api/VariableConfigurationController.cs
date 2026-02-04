using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.Filters;
using BrandVue.MixPanel;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using BrandVue.Variable;
using IronPython.Runtime.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Threading;
using static BrandVue.MixPanel.MixPanel;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api/meta")]
    [CacheControl(NoStore = true)]
    public class VariableConfigurationController : ApiController
    {
        private readonly IReadableVariableConfigurationRepository _variableConfigurationRepository;
        private readonly IReadableMetricConfigurationRepository _metricConfigurationRepository;
        private readonly INetManager _netManager;
        private readonly IVariableManager _variableManager;
        private readonly ISubsetRepository _subsetRepository;
        private readonly ISampleSizeProvider _sampleSizeProvider;
        private readonly IMeasureRepository _measureRepository;
        private readonly IEntityRepository _entityRepository;
        private readonly IFieldExpressionParser _fieldExpressionParser;
        private readonly IUserContext _userContext;

        public VariableConfigurationController(IReadableVariableConfigurationRepository variableConfigurationRepository,
            IReadableMetricConfigurationRepository metricConfigurationRepository,
            INetManager netManager,
            IVariableManager variableManager,
            ISubsetRepository subsetRepository,
            ISampleSizeProvider sampleSizeProvider,
            IMeasureRepository measureRepository,
            IEntityRepository entityRepository,
            IFieldExpressionParser fieldExpressionParser,
            IUserContext userContext)
        {
            _variableConfigurationRepository = variableConfigurationRepository;
            _metricConfigurationRepository = metricConfigurationRepository;
            _netManager = netManager;
            _variableManager = variableManager;
            _subsetRepository = subsetRepository;
            _sampleSizeProvider = sampleSizeProvider;
            _measureRepository = measureRepository;
            _entityRepository = entityRepository;
            _fieldExpressionParser = fieldExpressionParser;
            _userContext = userContext;
        }

        [HttpGet]
        [Route("getvariables")]
        public IEnumerable<VariableConfigurationModel> GetVariables()
        {
            return _variableConfigurationRepository.GetAll().Select(_variableManager.ConvertToModel);
        }

        [HttpGet]
        [Route("getBaseVariables")]
        public IEnumerable<VariableConfigurationModel> GetBaseVariables()
        {
            return _variableConfigurationRepository.GetBaseVariables().Select(_variableManager.ConvertToModel);
        }

        [HttpGet]
        [Route("getFieldVariables")]
        public IReadOnlyCollection<VariableInstanceModel> GetFieldVariables()
        {
            return _fieldExpressionParser.GetDeclaredVariableModels();
        }

        [HttpGet]
        [Route("getQuestionVariableDefinitionVariables")]
        public IEnumerable<VariableConfigurationModel> GetQuestionVariableDefinitionVariables()
        {
            return _variableConfigurationRepository.GetAll()
                .Where(v => v.Definition is QuestionVariableDefinition _).Select(_variableManager.ConvertToModel);
        }

        [HttpGet]
        [Route("getvariableconfig")]
        public VariableConfigurationModel GetVariableConfiguration(string name)
        {
            var metricConfiguration = _metricConfigurationRepository.Get(name);
            if (!metricConfiguration.VariableConfigurationId.HasValue)
            {
                throw new NotFoundException($"Unable to locate variable {name}");
            }

            var dbConfiguration = _variableConfigurationRepository.Get(metricConfiguration.VariableConfigurationId.Value);
            return _variableManager.ConvertToModel(dbConfiguration);
        }

        [HttpPost]
        [Route("variableconfig")]
        [Authorize(Policy = FeatureRolePolicy.VariablesCreate)]
        public async Task<CreateVariableResultModel> Create([FromBody] VariableConfigurationCreateModel model)
        {
            await TrackAsync(new TrackAsyncEventModel(
                VueEvents.CreateVariable,
                _userContext.UserId,
                GetClientIpAddress()));
            var createVariableResultModel = _variableManager.ConstructVariableAndRelatedMetadata(model);
            return createVariableResultModel;
        }

        [HttpPost]
        [Route("flattenMultiEntityVariable")]
        [Authorize(Policy = FeatureRolePolicy.VariablesCreate)]
        public IEnumerable<CreateVariableResultModel> FlattenMultiEntityVariable([FromBody] VariableConfigurationCreateModel model)
        {
            return _variableManager.CreateFlattenedVariables(model);
        }

        [HttpPost]
        [Route("getVariableGroupCountAndSamplePreview")]
        [Authorize(Policy = FeatureRolePolicy.VariablesCreate_OR_VariableEdit)]
        [SubsetAuthorisation]
        public Task<IEnumerable<VariableSampleResult>> GetVariableGroupCountAndSamplePreview(
            [FromBody] VariableGroupSampleModel model, CancellationToken cancellationToken)
        {
            var measure = _variableManager.ConstructTemporaryVariableSampleMeasure(model.Group);
            return GetSamplePreview(measure, model.SubsetId, cancellationToken);
        }

        [HttpPost]
        [Route("getFieldExpressionVariableCountAndSamplePreview")]
        [Authorize(Policy = FeatureRolePolicy.VariablesCreate_OR_VariableEdit)]
        [SubsetAuthorisation]
        public async Task<IEnumerable<VariableSampleResult>> GetFieldExpressionVariableCountAndSamplePreview(
            [FromBody] VariableFieldExpressionSampleModel model, CancellationToken cancellationToken)
        {
            try
            {
                var measure = _variableManager.ConstructTemporaryVariableSampleMeasure(model.Definition);
                return await GetSamplePreview(measure, model.SubsetId, cancellationToken);
            }
            catch (SyntaxWarningException ex)
            {
                throw new BadRequestException($"Issue in expression: {ex.Message}");
            }
            catch (NotSupportedException ex)
            {
                throw new BadRequestException($"Unsupported syntax in expression: {ex.Message}");
            }
            catch (SyntaxErrorException ex)
            {
                throw new BadRequestException($"Error in expression: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("basevariableconfig")]
        [Authorize(Policy = FeatureRolePolicy.VariablesCreate)]
        public async Task<int> CreateBaseVariable([FromBody] VariableConfigurationCreateModel model)
        {
            await TrackAsync(new TrackAsyncEventModel(
                VueEvents.CreateBaseVariable,
                _userContext.UserId,
                GetClientIpAddress()));
            return _variableManager.CreateBaseVariable(model);
        }

        [HttpPut]
        [Route("updatevariableconfig")]
        [Authorize(Policy = FeatureRolePolicy.VariablesEdit)]
        [InvalidateBrowserCache]
        public async Task Update(int variableId,
            string variableName,
            [FromBody] VariableDefinition variableDefinition,
            CalculationType? calculationType)
        {
            await TrackAsync(new TrackAsyncEventModel(
                VueEvents.UpdateVariable,
                _userContext.UserId,
                GetClientIpAddress()));
            _variableManager.UpdateVariable(variableId, variableName, variableDefinition, calculationType);
        }

        [HttpPost]
        [Route("removenet")]
        [Authorize(Policy = FeatureRolePolicy.ReportsAddEdit)]
        [SubsetAuthorisation("selectedSubsetId")]
        public IActionResult RemoveNet(string selectedSubsetId, int partId, string metricName, int netVariableId, int optionToRemove)
        {
            _netManager.RemoveNet(selectedSubsetId, partId, metricName, netVariableId, optionToRemove);
            return Ok();
        }

        [HttpPost]
        [Route("addNet")]
        [Authorize(Policy = FeatureRolePolicy.ReportsAddEdit)]
        [SubsetAuthorisation("selectedSubsetId")]
        public IActionResult AddNet(
            string selectedSubsetId,
            int partId,
            string metricName,
            string netName,
            int[] nettedEntityInstanceIds)
        {
            var metric = _metricConfigurationRepository.Get(metricName);
            if (metric.OriginalMetricName.IsNullOrWhiteSpace())
            {
                _netManager.CreateNewNet(selectedSubsetId, metric, partId, netName, nettedEntityInstanceIds);
            }
            else
            {
                _netManager.AddGroupToNet(metric, partId, netName, nettedEntityInstanceIds);
            }

            return Ok();
        }

        [HttpPost]
        [Route("deleteAllVariablesById")]
        [Authorize(Policy = FeatureRolePolicy.VariablesDelete)]
        public async Task<IActionResult> DeleteVariableById(int variableId)
        {
            await TrackAsync(new TrackAsyncEventModel(
                VueEvents.DeleteVariable,
                _userContext.UserId,
                GetClientIpAddress()));
            _variableManager.DeleteVariableById(variableId);
            return new OkResult();
        }

        [HttpPost]
        [Route("deleteAllBaseVariablesById")]
        [Authorize(Policy = FeatureRolePolicy.VariablesDelete)]
        public async Task<IActionResult> DeleteBaseVariableById(int variableId)
        {
            await TrackAsync(new TrackAsyncEventModel(
                VueEvents.DeleteBaseVariable,
                _userContext.UserId,
                GetClientIpAddress()));
            _variableManager.DeleteBaseVariableById(variableId);
            return new OkResult();
        }

        [HttpGet]
        [Route("isVariableReferencedByAnotherVariable")]
        [Authorize(Policy = FeatureRolePolicy.VariablesCreate_OR_VariableEdit_OR_VariableDelete)]
        public VariableWarningModel[] IsVariableReferencedByAnotherVariable(int variableId)
        {
            return _variableManager.CheckVariableIsInUse(variableId);
        }

        private async Task<IEnumerable<VariableSampleResult>> GetSamplePreview(Measure measure, string subsetId,
            CancellationToken cancellationToken)
        {
            var subset = _subsetRepository.Get(subsetId);
            var weightingMeasures = new WeightingMetrics(_measureRepository, _entityRepository, subset, null);
            var entityCount = measure.EntityCombination.Count();
            if (entityCount > 1)
            {
                var results = await _sampleSizeProvider.GetUnweightedEntityResultAndSampleMultiEntityEstimate(subset, measure, new AlwaysIncludeFilter(), weightingMeasures, cancellationToken);
                return results.OrderBy(r => r.Instance.Id)
                    .Select(r => new VariableSampleResult(r.Result?.Result ?? 0.0, r.Result?.SampleSize ?? 0, SplitByEntityInstanceName: r.Instance.Name, HasMultiEntityFilterInstances: true));
            }
            else if (entityCount > 0)
            {
                var results = await _sampleSizeProvider.GetUnweightedEntityResultAndSample(subset, measure, new AlwaysIncludeFilter(), weightingMeasures, cancellationToken);
                return results.OrderBy(r => r.Instance.Id)
                    .Select(r => new VariableSampleResult(r.Result?.Result ?? 0.0, r.Result?.SampleSize ?? 0, SplitByEntityInstanceName: r.Instance.Name));
            }
            else
            {
                var result = await _sampleSizeProvider.GetUnweightedProfileResultAndSample(subset, measure, new AlwaysIncludeFilter(), weightingMeasures, cancellationToken);
                return new[] { new VariableSampleResult(result?.Result ?? 0.0, result?.SampleSize ?? 0) };
            }
        }
    }
}
