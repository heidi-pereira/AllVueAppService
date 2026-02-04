using System;
using System.Linq;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Expressions;

namespace BrandVue.SourceData.Measures;

public class MetricConfigurationFactory : IMetricConfigurationFactory
{
    private readonly IBaseExpressionGenerator _baseExpressionGenerator;

    public MetricConfigurationFactory(IBaseExpressionGenerator baseExpressionGenerator)
    {
        _baseExpressionGenerator = baseExpressionGenerator;
    }

    public MetricConfiguration CreateNewMetricForVariable(VariableConfiguration variable, CalculationType calculationType, IProductContext productContext)
    {
        var newMetric = new MetricConfiguration
        {
            Name = variable.Identifier,
            HelpText = variable.DisplayName,
            VariableConfigurationId = variable.Id,
            DisableMeasure = false,
            EligibleForCrosstabOrAllVue = true,
            CalcType = CalculationTypeParser.AsString(calculationType),
            BaseExpression = productContext.IsAllVue ? null : _baseExpressionGenerator.GetAnsweredQuestionPythonExpression(variable),
            BaseVals = productContext.IsAllVue ? "-99>1" : null,
            NumFormat = GetNumFormatForCalcType(calculationType),
            DisableFilter = !productContext.IsAllVue,
            Subset = null,
            ProductShortCode = productContext.ShortCode,
            SubProductId = productContext.SubProductId,
            VarCode = variable.Identifier,
            DisplayName = variable.DisplayName,
            EligibleForMetricComparison = false,
            HasData = true
        };

        if (variable.Definition is GroupedVariableDefinition groupedDefinition)
        {
            UpdateVariableGroupValuesForMetric(newMetric, groupedDefinition);
        }
        return newMetric;
    }

    public void UpdateMetricForVariable(MetricConfiguration metric, VariableConfiguration newVariable, string originalName, string newVariableName, CalculationType? calculationType)
    {
        if (originalName != newVariableName)
        {
            metric.DisplayName = newVariableName;
        }

        if (calculationType.HasValue)
        {
            metric.CalcType = CalculationTypeParser.AsString(calculationType.Value);
            metric.NumFormat = GetNumFormatForCalcType(calculationType.Value);
        }

        if (newVariable.Definition is GroupedVariableDefinition definition)
        {
            UpdateVariableGroupValuesForMetric(metric, definition);
        }
        else if (newVariable.Definition is SingleGroupVariableDefinition singleGroupVariableDefinition)
        {
            UpdateVariableGroupValuesForMetric(metric, singleGroupVariableDefinition);
        }
    }

    private void UpdateVariableGroupValuesForMetric(MetricConfiguration metricConfig, GroupedVariableDefinition groupedDefinition)
    {
        var values = string.Join(">", groupedDefinition.Groups.Min(g => g.ToEntityInstanceId), groupedDefinition.Groups.Max(g => g.ToEntityInstanceId));
        var filterValues = string.Join("|", groupedDefinition.Groups.Select(g => $"{g.ToEntityInstanceId}:{g.ToEntityInstanceName}"));
        metricConfig.BaseVals = values;
        metricConfig.TrueVals = values;
        metricConfig.FilterValueMapping = filterValues;
    }

    private void UpdateVariableGroupValuesForMetric(MetricConfiguration metricConfig, SingleGroupVariableDefinition singleGroupVariableDefinition)
    {
        //Changing a grouped variable to a single group variable could leave residual FilterValueMappings that overwrite entity instance names incorrectly, clear them.
        //May need to figure out the actual entity instances and use those for FilterValueMapping instead, but better to stop using FilterValueMapping and use entities instead
        metricConfig.BaseVals = null;
        metricConfig.TrueVals = null;
        metricConfig.FilterValueMapping = null;
    }

    private string GetNumFormatForCalcType(CalculationType calculationType)
    {
        switch (calculationType)
        {
            case CalculationType.YesNo:
                return MetricNumberFormatter.PercentageInputNumberFormat;
            case CalculationType.Average:
                return MetricNumberFormatter.IntegerInputNumberFormat;
            case CalculationType.NetPromoterScore:
                return MetricNumberFormatter.SignedIntegerInputNumberFormat;
            case CalculationType.Special_ShouldNotBeUsed:
            case CalculationType.Text:
            case CalculationType.EoTotalSpendPerTimeOfDay:
            case CalculationType.EoTotalSpendPerLocation:
            default:
                throw new BadRequestException($"Unable to get num format for {calculationType}, as currently unsupported");
        }
    }
}