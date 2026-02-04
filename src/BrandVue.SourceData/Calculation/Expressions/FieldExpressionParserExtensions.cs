using BrandVue.SourceData.Measures;

namespace BrandVue.SourceData.Calculation.Expressions
{
    internal static class FieldExpressionParserExtensions
    {
        public static Measure CreatePopulationMeasure(this IFieldExpressionParser fieldExpressionParser) =>
            new()
            {
                Name = "AlwaysOneMeasure",
                BaseExpression = fieldExpressionParser.ParseUserBooleanExpression("1"),
                PrimaryVariable = fieldExpressionParser.ParseUserNumericExpressionOrNull("1")
            };
    }
}