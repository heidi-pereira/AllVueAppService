using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Metrics;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.Models;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Variable;

namespace BrandVue.Services
{
    public interface IConfigureMetricService
    {
        void UpdateMetricDisabled(string metricName, bool disableMeasure);
        void UpdateMetricFilterDisabled(string metricName, bool disableFilterForMeasure);
        void UpdateEligibleForCrosstabOrAllVue(string metricName, bool updatedValue);
        void UpdateMetricDefaultSplitBy(string metricName, string entityTypeName);
        void UpdateMetricModalData(MetricModalDataModel model);
        void ConvertCalculationType(string metricName, CalculationType convertToCalculationType, string subsetId);
        void UpdateBaseVariable(string metricName, int? baseVariableId);
    }

    public class ConfigureMetricService : IConfigureMetricService
    {
        private readonly IMetricConfigurationRepository _metricConfigRepository;
        private readonly IMeasureRepository _measureRepository;
        private readonly IVariableConfigurationRepository _variableConfigurationRepository;
        private IMetricFactory _metricFactory;
        private readonly IVariableValidator _variableValidator;
        private readonly IMetricValidator _metricValidator;

        public ConfigureMetricService(IMetricConfigurationRepository metricConfigRepository,
            IMeasureRepository measureRepository,
            IVariableConfigurationRepository variableConfigurationRepository,
            IMetricFactory metricFactory,
            IVariableValidator variableValidator,
            IMetricValidator metricValidator)
        {
            _metricConfigRepository = metricConfigRepository;
            _measureRepository = measureRepository;
            _variableConfigurationRepository = variableConfigurationRepository;
            _metricFactory = metricFactory;
            _variableValidator = variableValidator;
            _metricValidator = metricValidator;
        }

        public void ConvertCalculationType(string metricName, CalculationType convertToCalculationType, string subsetId)
        {
            var metricConfig = GetMetricConfiguration(metricName);
            metricConfig.CalcType = CalculationTypeParser.AsString(convertToCalculationType);

            if (convertToCalculationType == CalculationType.NetPromoterScore)
            {
                metricConfig.BaseVals = metricConfig.TrueVals = "0>10";
                metricConfig.FilterValueMapping = "0,1,2,3,4,5,6:Detractors|7,8:Passives|9,10:Promoters";
                metricConfig.NumFormat = MetricNumberFormatter.SignedDecimalInputNumberFormat;
                _metricConfigRepository.Update(metricConfig);
                UpdateMeasure(metricConfig);
            }

            else if (convertToCalculationType == CalculationType.Average)
            {
                var measure = _measureRepository.Get(metricName);
                var question = measure.Field?.GetDataAccessModelOrNull(subsetId)?.QuestionModel;
                int min = int.MinValue + 1;
                int max = int.MaxValue;
                if ((question?.MinimumValue).HasValue && (question?.MaximumValue).HasValue && question.MaximumValue.Value > question.MinimumValue.Value)
                {
                    min = question.MinimumValue.Value;
                    max = question.MaximumValue.Value;
                }
                var values = string.Join(">", min, max);
                metricConfig.BaseVals = metricConfig.TrueVals = values;
                metricConfig.FilterValueMapping = "Range:Range";
                metricConfig.NumFormat = MetricNumberFormatter.DecimalInputNumberFormat;
                _metricConfigRepository.Update(metricConfig);
                UpdateMeasure(metricConfig);
            }

            else
            {
                throw new BadRequestException($"Unable to update {metricName} to {convertToCalculationType}");
            }
        }

        public MetricConfiguration GetMetricConfiguration(string metricName)
        {
            return _metricConfigRepository.Get(metricName) ?? throw new BadRequestException($"Could not find matching metric {metricName}");
        }

        public void UpdateEligibleForCrosstabOrAllVue(string metricName, bool updatedValue)
        {
            var metricConfig = GetMetricConfiguration(metricName);
            var measure = _measureRepository.Get(metricConfig.Name);
            metricConfig.EligibleForCrosstabOrAllVue = updatedValue;
            metricConfig.HasData = true;
            _metricConfigRepository.Update(metricConfig);
            measure.EligibleForCrosstabOrAllVue = updatedValue;
        }

        private void UpdateMeasure(MetricConfiguration metricConfig)
        {
            var measure = _measureRepository.Get(metricConfig.Name);
            _metricFactory.LoadMetric(metricConfig, measure);
        }

        public void UpdateMetricDefaultSplitBy(string metricName, string entityTypeName)
        {
            var metricConfig = GetMetricConfiguration(metricName);
            var measure = _measureRepository.Get(metricConfig.Name);
            metricConfig.DefaultSplitByEntityType = entityTypeName;
            _metricConfigRepository.Update(metricConfig);
            measure.DefaultSplitByEntityTypeName = entityTypeName;
        }

        public void UpdateMetricDisabled(string metricName, bool disableMeasure)
        {
            var metricConfig = GetMetricConfiguration(metricName);
            var measure = _measureRepository.Get(metricConfig.Name);
            metricConfig.DisableMeasure = disableMeasure;
            _metricConfigRepository.Update(metricConfig);
            measure.DisableMeasure = disableMeasure;
        }

        public void UpdateMetricFilterDisabled(string metricName, bool disableFilterForMeasure)
        {
            var metricConfig = GetMetricConfiguration(metricName);
            var measure = _measureRepository.Get(metricConfig.Name);
            metricConfig.DisableFilter = disableFilterForMeasure;
            _metricConfigRepository.Update(metricConfig);
            measure.DisableFilter = disableFilterForMeasure;
        }

        public void UpdateMetricModalData(MetricModalDataModel model)
        {
            var displayName = model.DisplayName.Trim();
            var originalMetric = GetMetricConfiguration(model.MetricName);
            var originalVariable = null as VariableConfiguration;
            try
            {
                UpdateMetricFromModalData(displayName, model.DisplayText, model.EntityInstanceIdMeanCalculationValueMapping, originalMetric);
                if (originalMetric.VariableConfigurationId is not null)
                {
                    originalVariable = _variableConfigurationRepository.Get(originalMetric.VariableConfigurationId.Value);
                    UpdateVariableDisplayName(displayName, originalVariable);
                }
            }
            catch (BadRequestException e)
            {
                //if we had any issues revert the changes
                if (originalVariable is not null)
                {
                    _variableConfigurationRepository.Update(originalVariable);
                }
                _metricConfigRepository.Update(originalMetric);
                throw new BadRequestException($"Error updating metric {model.MetricName}: {e.Message}");
            }
        }

        private void UpdateVariableDisplayName(string displayName, VariableConfiguration originalVariable)
        {
            bool isSameName = string.Equals(originalVariable.DisplayName, displayName, System.StringComparison.InvariantCultureIgnoreCase);
            if (originalVariable is not null && !isSameName)
            {
                var newVariable = originalVariable with { DisplayName = displayName };
                _variableConfigurationRepository.Update(newVariable);
            }
        }

        private void UpdateMetricFromModalData(string displayName, string helpText, string entityInstanceIdMeanCalculationValueMapping, MetricConfiguration originalMetric)
        {
            var newMetric = originalMetric.ShallowCopy();
            if (!string.Equals(originalMetric.DisplayName, displayName, System.StringComparison.InvariantCultureIgnoreCase))
            {
                _metricValidator.ValidateMetricDisplayName(displayName);
                newMetric.DisplayName = displayName;
            }
            newMetric.HelpText = helpText;
            newMetric.EntityInstanceIdMeanCalculationValueMapping = entityInstanceIdMeanCalculationValueMapping;

            _metricConfigRepository.Update(newMetric);
        }

        public void UpdateBaseVariable(string metricName, int? baseVariableId)
        {
            var metricConfig = GetMetricConfiguration(metricName);
            metricConfig.BaseVariableConfigurationId = baseVariableId;
            _metricConfigRepository.Update(metricConfig);
        }
    }
}
