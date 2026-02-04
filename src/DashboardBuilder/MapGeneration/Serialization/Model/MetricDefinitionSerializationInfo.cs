using System.Collections.Generic;
using System.Linq;
using MIG.SurveyPlatform.MapGeneration.Model;

namespace MIG.SurveyPlatform.MapGeneration.Serialization.Model
{
    internal class MetricDefinitionSerializationInfo : ISerializationInfo<MetricDefinition>
    {
        public string SheetName { get; } = "Metrics";

        public string[] ColumnHeadings { get; } = new[]
        {
            nameof(MetricDefinition.Name), nameof(MetricDefinition.Type), nameof(MetricDefinition.BaseResponseType), nameof(MetricDefinition.Field), nameof(MetricDefinition.Field2), nameof(MetricDefinition.FieldOp), nameof(MetricDefinition.CalcType), nameof(MetricDefinition.TrueVals), nameof(MetricDefinition.BaseField), nameof(MetricDefinition.BaseVals), nameof(MetricDefinition.KeyImage), nameof(MetricDefinition.Measure), nameof(MetricDefinition.HelpText), nameof(MetricDefinition.NumFormat), nameof(MetricDefinition.Min), nameof(MetricDefinition.Max), nameof(MetricDefinition.ExcludeWaves), nameof(MetricDefinition.StartDate), nameof(MetricDefinition.FilterValueMapping), nameof(MetricDefinition.FilterGroup), nameof(MetricDefinition.PreNormalisationMinimum), nameof(MetricDefinition.PreNormalisationMaximum), nameof(MetricDefinition.Subset), nameof(MetricDefinition.DisableMeasure), nameof(MetricDefinition.DisableFilter), nameof(MetricDefinition.Environment)
        };

        public string[] RowData(MetricDefinition m)
        {
            return new[]
            {
                m.Name, m.Type, m.BaseResponseType, m.Field, m.Field2, m.FieldOp, m.CalcType, m.TrueVals, m.BaseField, m.BaseVals, m.KeyImage, m.Measure, m.HelpText, m.NumFormat,
                m.Min.ToString(), m.Max.ToString(), m.ExcludeWaves, m.StartDate, m.FilterValueMapping, m.FilterGroup, m.PreNormalisationMinimum, m.PreNormalisationMaximum,
                m.Subset, m.DisableMeasure, m.DisableFilter, m.Environment
            };
        }

        public IEnumerable<MetricDefinition> OrderForOutput(IEnumerable<MetricDefinition> metricDefinitions)
        {
            return metricDefinitions.OrderBy(f => f.Field, NaturalStringComparer.Instance).ThenBy(f => f.Name, NaturalStringComparer.Instance);
        }
    }
}