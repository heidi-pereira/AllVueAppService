using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.Models;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Variable;
using BrandVue.Variable;
using System.Threading;

namespace BrandVue.Services
{
    public interface IVariableManager
    {
        CreateVariableResultModel ConstructVariableAndRelatedMetadata(VariableConfigurationCreateModel model);
        int CreateBaseVariable(VariableConfigurationCreateModel model);
        void UpdateVariable(int variableId, string newVariableName, VariableDefinition newVariableDefinition, CalculationType? calculationType);
        void DeleteVariableById(int VariableconfigId );
        void DeleteBaseVariableById(int VariableconfigId );
        void UpdateVariableGroupValuesForMetric(MetricConfiguration metricConfig, GroupedVariableDefinition groupedDefinition);
        MultipleEntitySplitByAndFilterBy GetSplitByAndFilterBy(IReadOnlyCollection<string> entityTypeNames, string splitByEntityTypeName = null);
        VariableWarningModel[] CheckVariableIsInUse(int variableId);
        Measure ConstructTemporaryVariableSampleMeasure(VariableGrouping group);
        Measure ConstructTemporaryVariableSampleMeasure(FieldExpressionVariableDefinition definition);
        Measure ConstructTemporaryVariableMeasure(GroupedVariableDefinition definition);
        VariableConfigurationModel ConvertToModel(VariableConfiguration dbConfiguration);
        IEnumerable<CreateVariableResultModel> CreateFlattenedVariables(VariableConfigurationCreateModel model);
    }
}