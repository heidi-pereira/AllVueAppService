namespace BrandVue.SourceData.Measures
{
    public static class MeasureExtension
    {
        public static bool IsVariableWithoutBaseExpression(this Measure measure)
        {
            return (measure.BaseExpression == null) && (measure.BaseField == null) && (measure.BaseVariableConfigurationId == null) && (measure.IsBasedOnCustomVariable);
        }
    }
}
