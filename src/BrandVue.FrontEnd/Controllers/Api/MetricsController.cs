using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.Filters;
using BrandVue.MixPanel;
using BrandVue.Services.Exporter;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using Vue.Common.Constants.Constants;
using static BrandVue.MixPanel.MixPanel;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api/meta")]
    [CacheControl(NoStore = true)]
    [NbspFilter]
    public class MetricsController : ApiController
    {
        private readonly ISubsetRepository _subsetRepository;
        private readonly IMeasureRepository _measureRepository;
        private readonly IMetricConfigurationRepository _metricConfigRepository;
        private readonly IMetricAboutRepository _metricAboutRepository;
        private readonly IUserContext _userContext;
        private readonly ILinkedMetricRepository _linkedMetricRepository;
        private readonly IMeasureBaseDescriptionGenerator _measureBaseDescriptionGenerator;
        private readonly IVariableConfigurationRepository _variableConfigurationRepository;
        private readonly IVariableConfigurationFactory _variableConfigurationFactory;
        private readonly IVariableValidator _variableValidator;

        public MetricsController(IMeasureRepository measureRepository,
            IMetricConfigurationRepository metricConfigRepository,
            IMetricAboutRepository metricAboutRepository,
            ISubsetRepository subsetRepository,
            IUserContext userContext,
            ILinkedMetricRepository linkedMetricRepository,
            IMeasureBaseDescriptionGenerator measureBaseDescriptionGenerator,
            IVariableConfigurationRepository variableConfigurationRepository,
            IVariableConfigurationFactory variableConfigurationFactory,
            IVariableValidator variableValidator)
        {
            _measureRepository = measureRepository;
            _metricConfigRepository = metricConfigRepository;
            _metricAboutRepository = metricAboutRepository;
            _subsetRepository = subsetRepository;
            _userContext = userContext;
            _linkedMetricRepository = linkedMetricRepository;
            _measureBaseDescriptionGenerator = measureBaseDescriptionGenerator;
            _variableConfigurationRepository = variableConfigurationRepository;
            _variableConfigurationFactory = variableConfigurationFactory;
            _variableValidator = variableValidator;
        }

        [HttpGet]
        [Route("metrics")]
        [SubsetAuthorisation(nameof(selectedSubset))]
        public IEnumerable<Measure> GetMetrics(string selectedSubset)
        {
            return _measureRepository.GetAllMeasuresWithDisabledPropertyFalseForSubset(_subsetRepository.Get(selectedSubset));
        }

        [HttpGet]
        [Route("metricsWithDisabled")]
        [SubsetAuthorisation(nameof(selectedSubset))]
        public IEnumerable<Measure> GetMetricsWithDisabled(string selectedSubset)
        {
            return _measureRepository.GetAllMeasuresIncludingDisabledForSubset(_subsetRepository.Get(selectedSubset));
        }

        [HttpGet]
        [Route("metricsWithDisabledAndBase")]
        [SubsetAuthorisation(nameof(selectedSubset))]
        public IEnumerable<Measure> GetMetricsWithDisabledAndBaseDescription(string selectedSubset)
        {
            var subset = _subsetRepository.Get(selectedSubset);
            var includedMeasures = _measureRepository.GetAllMeasuresIncludingDisabledForSubset(subset);
            return includedMeasures.Select(measure =>
            {
                var (baseDescription, hasCustomBase) = _measureBaseDescriptionGenerator.GetBaseDescriptionAndHasCustomBase(measure, subset);
                measure.SubsetSpecificBaseDescription = baseDescription;
                measure.HasCustomBase = hasCustomBase;
                return measure;
            });
        }

        [HttpGet]
        [Route("metricsWithDisabledForAllSubsets")]
        public IEnumerable<Measure> GetMetricsWithDisabledForAllSubsets()
        {
            return _measureRepository.GetAllForCurrentUser();
        }

        [HttpGet]
        [Route("metricConfigurations")]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        public IEnumerable<MetricConfiguration> GetMetricConfigurations()
        {
            var metricConfigurations = _metricConfigRepository.GetAll().ToArray();
            var variables = _variableConfigurationRepository.GetAll().ToDictionary(v => v.Id);
            for (var i = 0; i < metricConfigurations.Length; i++)
            {
                var metric = metricConfigurations[i];
                if (metric.VariableConfigurationId.HasValue)
                {
                    if (variables.TryGetValue(metric.VariableConfigurationId.Value, out var variableConfig) && variableConfig.Definition is FieldExpressionVariableDefinition fieldExpressionDefinition)
                    {
                        metric = metric.ShallowCopy();
                        metric.FieldExpression = fieldExpressionDefinition.Expression;
                        metricConfigurations[i] = metric;
                    }
                }
            }
            return metricConfigurations;
        }

        [HttpPost]
        [Route("metricConfigurations")]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        public async Task<MetricConfiguration> CreateMetricConfiguration([FromBody] MetricConfiguration metricConfiguration)
        {
            await TrackAsync(new TrackAsyncEventModel(
                VueEvents.CreatedNewMetric,
                _userContext.UserId,
                GetClientIpAddress()));
            VariableConfiguration variableConfig = null;
            PopulateVariables(metricConfiguration); //Would be better to use a separate model for the API - deserializing db models often leads to a mess
            if (metricConfiguration.FieldExpression != null)
            {
                variableConfig = CreateFieldExpressionVariableConfiguration(metricConfiguration.Name, metricConfiguration.FieldExpression);
                metricConfiguration.VariableConfigurationId = variableConfig.Id;
            }
            try
            {
                var fieldExpressionToReturn = metricConfiguration.FieldExpression;
                metricConfiguration.FieldExpression = null;
                _metricConfigRepository.Create(metricConfiguration);

                var copy = metricConfiguration.ShallowCopy();
                copy.FieldExpression = fieldExpressionToReturn;
                return copy;
            }
            catch (Exception)
            {
                if (variableConfig != null)
                {
                    _variableConfigurationRepository.Delete(variableConfig);
                }
                throw;
            }
        }

        [HttpPut]
        [Route("metricConfigurations/{metricId}")]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        [InvalidateBrowserCache]
        public async Task<MetricConfiguration> UpdateMetricConfiguration(int metricId, [FromBody] MetricConfiguration metricConfiguration)
        {
            await TrackAsync(new TrackAsyncEventModel(
                VueEvents.UpdatedMetric,
                _userContext.UserId,
                GetClientIpAddress()));

            VariableConfiguration variableConfig = null;
            string oldFieldExpression = null;
            string newFieldExpression = metricConfiguration.FieldExpression;
            PopulateVariables(metricConfiguration); //Would be better to use a separate model for the API - deserializing db models often leads to a mess
            var oldMetric = _metricConfigRepository.Get(metricId);
            await TrackMetricChanges(metricConfiguration, oldMetric);
            if (oldMetric.VariableConfigurationId.HasValue)
            {
                variableConfig = _variableConfigurationRepository.Get(oldMetric.VariableConfigurationId.Value);
                if (variableConfig is null)
                {
                    if (newFieldExpression != null)
                    {
                        variableConfig =
                            CreateFieldExpressionVariableConfiguration(metricConfiguration.Name, newFieldExpression);
                        metricConfiguration.VariableConfigurationId = variableConfig.Id;
                        oldFieldExpression = newFieldExpression;
                    }
                    else
                    {
                        metricConfiguration.VariableConfigurationId = null;
                    }
                }
                else if (variableConfig.Definition is FieldExpressionVariableDefinition fieldExpressionDefinition)
                {
                    oldFieldExpression = fieldExpressionDefinition.Expression;
                }
            }

            if (oldFieldExpression != newFieldExpression)
            {
                if (newFieldExpression == null)
                {
                    //delete happens after creating metric to prevent needing to rollback a delete
                }
                else if (variableConfig == null)
                {
                    //create new variable
                    variableConfig = CreateFieldExpressionVariableConfiguration(metricConfiguration.Name, newFieldExpression);
                    metricConfiguration.VariableConfigurationId = variableConfig.Id;
                }
                else if (variableConfig.Definition is FieldExpressionVariableDefinition fieldExpressionDefinition)
                {
                    //update variable to use the new expression
                    fieldExpressionDefinition.Expression = newFieldExpression;
                    _variableValidator.Validate(variableConfig, out _, out _);
                    _variableConfigurationRepository.Update(variableConfig);
                }
                else
                {
                    //incompatible variable types
                    throw new BadRequestException("Cannot override a variable to use a expression string, the variable should be removed/deleted first");
                }
            }

            try
            {
                metricConfiguration.Id = metricId;
                metricConfiguration.FieldExpression = null;
                if (oldFieldExpression != null && newFieldExpression == null)
                {
                    metricConfiguration.VariableConfigurationId = null;
                }
                _metricConfigRepository.Update(metricConfiguration);
                if (oldFieldExpression != null && newFieldExpression == null)
                {
                    _variableConfigurationRepository.Delete(variableConfig);
                }
                return metricConfiguration;
            }
            catch (Exception)
            {
                if (oldFieldExpression != newFieldExpression)
                {
                    if (oldFieldExpression == null && variableConfig != null && variableConfig.Definition is FieldExpressionVariableDefinition)
                    {
                        _variableConfigurationRepository.Delete(variableConfig);
                    }
                    else if (oldFieldExpression != null && newFieldExpression != null)
                    {
                        ((FieldExpressionVariableDefinition)variableConfig.Definition).Expression = oldFieldExpression;
                        _variableConfigurationRepository.Update(variableConfig);
                    }
                }
                throw;
            }
        }

        private void PopulateVariables(MetricConfiguration metricConfiguration)
        {
            if (metricConfiguration.VariableConfigurationId.HasValue)
            {
                metricConfiguration.VariableConfiguration = _variableConfigurationRepository.Get(metricConfiguration.VariableConfigurationId.Value);
            }

            if (metricConfiguration.BaseVariableConfigurationId.HasValue)
            {
                metricConfiguration.BaseVariableConfiguration = _variableConfigurationRepository.Get(metricConfiguration.BaseVariableConfigurationId.Value);
            }
        }

        private async Task TrackMetricChanges(MetricConfiguration metricConfiguration, MetricConfiguration oldMetric)
        {
            if (oldMetric.HelpText != metricConfiguration.HelpText)
            {
                await TrackAsync(new TrackAsyncEventModel(
                    VueEvents.EditedHelpText,
                    _userContext.UserId,
                    GetClientIpAddress()));
            }
            //9/7/24 - this event should no longer happen, but lets keep for a few weeks to make sure then remove
            if (oldMetric.VarCode != metricConfiguration.VarCode)
            {
                await TrackAsync(new TrackAsyncEventModel(
                    VueEvents.EditedVarCode,
                    _userContext.UserId,
                    GetClientIpAddress()));
            }
            if (oldMetric.DisplayName != metricConfiguration.DisplayName)
            {
                await TrackAsync(new TrackAsyncEventModel(
                    VueEvents.EditedMetricDisplayName,
                    _userContext.UserId,
                    GetClientIpAddress()));
            }
            if (metricConfiguration.DisableMeasure && metricConfiguration.DisableMeasure != oldMetric.DisableMeasure)
            {
                await TrackAsync(new TrackAsyncEventModel(
                    VueEvents.DisabledMetric,
                    _userContext.UserId,
                    GetClientIpAddress()));
            }
            if (!metricConfiguration.DisableMeasure && metricConfiguration.DisableMeasure != oldMetric.DisableMeasure)
            {
                await TrackAsync(new TrackAsyncEventModel(
                    VueEvents.EnabledMetric,
                    _userContext.UserId,
                    GetClientIpAddress()));
            }
        }

        private VariableConfiguration CreateFieldExpressionVariableConfiguration(string metricName, string fieldExpression)
        {
            var variableName = $"{metricName} (Variable Expression)";
            var definition = new FieldExpressionVariableDefinition
            {
                Expression = fieldExpression
            };
            var identifier = _variableConfigurationFactory.CreateIdentifierFromName(variableName);
            var variableConfig = _variableConfigurationFactory.CreateVariableConfigFromParameters(variableName, identifier, definition, out var dependencyVariableInstanceIdentifiers, out _);
            return _variableConfigurationRepository.Create(variableConfig, dependencyVariableInstanceIdentifiers);
        }

        [HttpDelete]
        [Route("metricConfigurations/{metricId}")]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        public async Task<HttpStatusCode> DeleteMetric(int metricId)
        {
            await TrackAsync(new TrackAsyncEventModel(
                VueEvents.DeletedMetric,
                _userContext.UserId,
                GetClientIpAddress()));

            var oldMetric = _metricConfigRepository.Get(metricId);
            _metricConfigRepository.Delete(metricId);
            if (oldMetric.VariableConfigurationId.HasValue)
            {
                var variableConfig = _variableConfigurationRepository.Get(oldMetric.VariableConfigurationId.Value);
                if (variableConfig.Definition is FieldExpressionVariableDefinition)
                {
                    _variableConfigurationRepository.Delete(variableConfig);
                }
                else if (variableConfig.Definition is QuestionVariableDefinition)
                {
                    _variableConfigurationRepository.Delete(variableConfig);
                }
            }
            return HttpStatusCode.OK;
        }

        [HttpPost]
        [Route("metricAboutsGet")]
        // HACK: This needs to be a POST request so that metric names that contain slashes are handled
        // the same way for creating and retrieving. And it needs to be on a separate route because
        // two POSTs can't go to the same route, overloading isn't smart enough to handle that.
        public IEnumerable<MetricAbout> GetMetricAbouts([FromBody] string metricName)
        {
            return _metricAboutRepository.GetAllForMetric(metricName);
        }

        [HttpPost]
        [Route("metricAbouts")]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        public MetricAbout CreateMetricAbout([FromBody] MetricAbout metricAbout)
        {
            metricAbout.User = _userContext.UserName;
            _metricAboutRepository.Create(metricAbout);
            return metricAbout;
        }

        [HttpPut]
        [Route("metricAbouts")]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        public MetricAbout[] UpdateMetricAboutList([FromBody] MetricAbout[] metricAbouts)
        {
            foreach (var metricAbout in metricAbouts)
            {
                metricAbout.User = _userContext.UserName;
            }

            _metricAboutRepository.UpdateList(metricAbouts);
            return metricAbouts;
        }

        [HttpDelete]
        [Route("metricAbouts")]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        public HttpStatusCode DeleteMetricAbout([FromBody] MetricAbout metricAbout)
        {
            // Update username so we can keep track of who deleted it
            metricAbout.User = _userContext.UserName;
            _metricAboutRepository.Delete(metricAbout);
            return HttpStatusCode.OK;
        }

        [HttpGet]
        [Route("linkedMetrics")]
        public async Task<IEnumerable<string>> GetLinkedMetrics(string metricName)
        {
            var linkedMetric = await _linkedMetricRepository.GetLinkedMetricsForMetric(metricName);
            if (linkedMetric == null)
            {
                return new string[] { };
            }

            return linkedMetric.LinkedMetricNames.AsEnumerable();
        }

        [HttpGet]
        [Route("MetricsCsvExport")]
        public FileStreamResult ExportMetricsToCsv()
        {
            var metrics = _metricConfigRepository.GetAll().ToList();

            Stream stream = new MemoryStream();

            CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                NewLine = NewLine.Environment
            };

            for (var i = 0; i < metrics.Count; i++)
            {
                var metric = metrics[i];
                if (metric.VariableConfigurationId.HasValue)
                {
                    var variable = _variableConfigurationRepository.Get(metric.VariableConfigurationId.Value);
                    if (variable != null && variable.Definition is FieldExpressionVariableDefinition fieldExpressionDefinition)
                    {
                        metrics[i] = metric.ShallowCopy();
                        metrics[i].FieldExpression = fieldExpressionDefinition.Expression;
                    }
                }
            }
            using TextWriter streamWriter = new StreamWriter(stream, Encoding.ASCII, 2048, true);
            using CsvWriter csvWriter = new CsvWriter(streamWriter, config, true);
            csvWriter.WriteRecords((IEnumerable)metrics);
            streamWriter.Flush();
            stream.Position = 0;
            return File(stream, ExportHelper.MimeTypes.Csv);
        }
    }
}
