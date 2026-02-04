using Aspose.Slides;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;

namespace BrandVue.Services.Exporter
{
    public class ExportAverageHelper: IExportAverageHelper
    {
        private readonly IMeasureRepository _measureRepository;
        private readonly IVariableConfigurationRepository _variableConfigurationRepository;
        private readonly IQuestionTypeLookupRepository _questionTypeLookupRepository;

        public ExportAverageHelper(IMeasureRepository measureRepository,
            IVariableConfigurationRepository variableConfigurationRepository,
            IQuestionTypeLookupRepository questionTypeLookupRepository)
        {
            _measureRepository = measureRepository;
            _variableConfigurationRepository = variableConfigurationRepository;
            _questionTypeLookupRepository = questionTypeLookupRepository;
        }

        public AverageType[] VerifyAverageTypesForMeasure(Measure measure,
            AverageType[] averageTypes,
            Subset subset)
        {
            var measureToCheck = GetUnderlyingMetric(measure);
            var questionTypeLookup = _questionTypeLookupRepository.GetForSubset(subset);
            var questionType = questionTypeLookup.TryGetValue(measureToCheck.Name, out var type) ? type : MainQuestionType.Unknown;
            return AverageHelper.VerifyAverageTypesForQuestionType(averageTypes, questionType);
        }

        private Measure GetUnderlyingMetric(Measure measure)
        {
            if (!measure.IsBasedOnCustomVariable || !measure.VariableConfigurationId.HasValue)
            {
                return measure;
            }

            var variable = _variableConfigurationRepository.Get(measure.VariableConfigurationId.Value);
            var variableDefinition = variable.Definition;
            if (variableDefinition is not GroupedVariableDefinition)
            {
                return measure;
            }

            var components = ((GroupedVariableDefinition)variableDefinition).Groups.Select(g => g.Component);
            if (!components.Any() || components.First() is not InstanceListVariableComponent)
            {
                return measure;
            }

            var firstComponent = (InstanceListVariableComponent)components.First();
            var fromVariableIdentifier = firstComponent.FromVariableIdentifier;

            var mixedComponentsFound = components.Any(c => c is InstanceListVariableComponent ic && ic.FromVariableIdentifier != fromVariableIdentifier);
            if (mixedComponentsFound)
            {
                return measure;
            }

            var measures = _measureRepository.GetAll();
            return measures.First(m => m.PrimaryVariableIdentifier == fromVariableIdentifier);
        }
    }
}
