using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;

namespace BrandVue.SourceData.Measures;

public interface IMetricConfigurationFactory
{
    MetricConfiguration CreateNewMetricForVariable(VariableConfiguration variable, CalculationType calculationType, IProductContext productContext);
    void UpdateMetricForVariable(MetricConfiguration metric, VariableConfiguration newVariable, string originalName, string newVariableName, CalculationType? calculationType);
}