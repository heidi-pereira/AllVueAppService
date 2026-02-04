using System.Collections.Generic;
using System.Linq;
using MIG.SurveyPlatform.MapGeneration.Model;
using MIG.SurveyPlatform.MapGeneration.Mqml;

namespace MIG.SurveyPlatform.MapGeneration.Metrics
{
    internal class MetricGenerator
    {
        private readonly MapData m_MapData;
        private const string DefaultValsNotIntersectingSpecialVals = "-98>1000000";
        private const string UncheckedCheckbox = "-99";
        private readonly FieldCollections m_FieldCollections;

        public MetricGenerator(MapData mapData)
        {
            m_MapData = mapData;
            m_FieldCollections = mapData.FieldCollections;
        }

        public IReadOnlyCollection<MetricDefinition> Generate()
        {
            return m_FieldCollections.BrandFields.Select(GenerateMetric).ToList();
        }

        private MetricDefinition GenerateMetric(FieldDefinition field)
        {
            var basicMetricDef = new MetricDefinition(field)
            {
                Name = field.Context.GetHumanName(": "),
                BaseResponseType = "brand",
                Type = "brand",
                Field = field.Name,
                NumFormat = "0",
                Min = field.Context.ScaleMin,
                Max = field.Context.ScaleMax,
                Measure = field.Question,
                Field2 = "",
                FieldOp = "",
                Subset = "" // Need to parse actions to see if page is skipped for a specific subset (and hence we should include all except the skipped subsets pipe separated in this list)
            };

            if (field.Context.IsRatingQuestion)
            {
                return WithIndexedAverage(field, basicMetricDef);
            }

            var possibleValues = field.Context.QuestionType == QQuestion.MainOptTypes.CheckBox || field.Context.QuestionType == QQuestion.MainOptTypes.Radio
                ? new[] {"1", UncheckedCheckbox}
                : GetIds(field.Context.Categories);

            return possibleValues.Count() > 1 ? WithYesNo(basicMetricDef, possibleValues, field.Context.IsImplicitlyAskedToEveryoneWhoSeesBrand) : WithAverage(basicMetricDef, DefaultValsNotIntersectingSpecialVals);
        }

        private string[] GetIds(IEnumerable<string> brandBaseVals)
        {
            return brandBaseVals.Select(s => s.Split(':').First()).ToArray();
        }

        private MetricDefinition WithIndexedAverage(FieldDefinition field, MetricDefinition basicMetricDef)
        {
            basicMetricDef.Min = 0;
            basicMetricDef.Max = 100;
            basicMetricDef.NumFormat = "0;-0;0";
            basicMetricDef.PreNormalisationMinimum = field.Context.ScaleMin.ToString();
            basicMetricDef.PreNormalisationMaximum = field.Context.ScaleMax.ToString();
            return WithAverage(basicMetricDef, $"{field.Context.ScaleMin}>{field.Context.ScaleMax}");
        }

        private MetricDefinition WithYesNo(MetricDefinition basicMetricDef, IReadOnlyCollection<string> possibleValues, bool isImplicitlyAskedToEveryoneWhoSeesBrand)
        {
            basicMetricDef.BaseField = isImplicitlyAskedToEveryoneWhoSeesBrand ? m_MapData.BrandAskedQuestion : basicMetricDef.Field;
            basicMetricDef.BaseVals = string.Join("|", isImplicitlyAskedToEveryoneWhoSeesBrand ? GetIds(m_MapData.BrandAskedChoices) : possibleValues);
            basicMetricDef.TrueVals = string.Join("|", possibleValues.First(v => !v.Equals(UncheckedCheckbox)));
            basicMetricDef.CalcType = "yn";
            return basicMetricDef;
        }

        private MetricDefinition WithAverage(MetricDefinition basicMetricDef, string avgVals)
        {
            basicMetricDef.BaseField = basicMetricDef.Field;
            basicMetricDef.BaseVals = avgVals;
            basicMetricDef.TrueVals = avgVals;
            basicMetricDef.CalcType = "avg";
            return basicMetricDef;
        }
    }
}