using System.Diagnostics.CodeAnalysis;

namespace BrandVue.SourceData.Calculation
{
    public class CategoryResult
    {
        public string MeasureName { get; set; }
        public string EntityInstanceName { get; set; }
        public double Result { get; set; }
        public double? AverageValue { get; set; }
        public int? BaseVariableConfigurationId { get; set; }

        public CategoryResult([NotNull] string measureName, string entityInstanceName, WeightedDailyResult dailyResult, double? averageValue = null, int? baseId = null)
        {
            MeasureName = measureName ?? throw new ArgumentNullException(nameof(measureName));
            EntityInstanceName = entityInstanceName;
            Result = dailyResult.WeightedResult;
            AverageValue = averageValue;
            BaseVariableConfigurationId = baseId;
        }
    }
}
