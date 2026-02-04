using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Models;

namespace BrandVue.Services.Exporter.ReportPowerpoint
{
    public static class ReportPowerpointHelper
    {
        public static int? GetMeanCalculationValueOrNull(int? entityInstanceId, EntityMeanMap entityInstanceIdMeanCalculationValueMapping)
        {
            int? meanValue = entityInstanceId;

            if (entityInstanceIdMeanCalculationValueMapping != null)
            {
                var mapping = entityInstanceIdMeanCalculationValueMapping.Mapping
                    .FirstOrDefault(m => m.EntityId == entityInstanceId);

                if (mapping != null)
                {
                    meanValue = mapping.IncludeInCalculation ? mapping.MeanCalculationValue : null;
                }
            }

            return meanValue;
        }

        public static Significance GetDisplayedSignificance(Significance? dataPointSignificance,
            DisplaySignificanceDifferences displaySignificanceDifferences)
        {
            if (!dataPointSignificance.HasValue || dataPointSignificance == Significance.None)
            {
                return Significance.None;
            }

            if (dataPointSignificance == Significance.Up &&
                (displaySignificanceDifferences == DisplaySignificanceDifferences.ShowUp ||
                 displaySignificanceDifferences == DisplaySignificanceDifferences.ShowBoth))
            {
                return Significance.Up;
            }

            if (dataPointSignificance == Significance.Down &&
                (displaySignificanceDifferences == DisplaySignificanceDifferences.ShowDown ||
                 displaySignificanceDifferences == DisplaySignificanceDifferences.ShowBoth))
            {
                return Significance.Down;
            }

            return Significance.None;
        }
    }
}

