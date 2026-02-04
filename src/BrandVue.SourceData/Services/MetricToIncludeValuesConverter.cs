using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Models.Filters;

namespace BrandVue.SourceData.Services
{
    public class MetricToIncludeValuesConverter
    {
        private const string STOP_GAP_EXPRESSION = "<expression>";
        
        public static FilterInfo GetFilterInfo(
            AllowedValues measureAllowedValues, 
            string identifier, 
            IEnumerable<EntityType> measureEntityCombination, 
            IEnumerable<ResponseFieldDescriptor> measurePrimaryFieldDependencies, 
            IVariable measureFieldExpression)
        {
            if (identifier is not null)
            {
                if (measureAllowedValues.IsList && !measureAllowedValues.IsRange)
                {
                    return new FilterInfoList(identifier, GetQuestionClassIds(measureEntityCombination))
                    {
                        IncludeList = measureAllowedValues.Values
                    };
                }

                if (measureAllowedValues.IsRange && !measureAllowedValues.IsList)
                {
                    return new FilterInfoRange(identifier, GetQuestionClassIds(measureEntityCombination))
                    {
                        Min = measureAllowedValues.Minimum ?? 0,
                        Max = measureAllowedValues.Maximum ?? 0,
                    };
                }
            }

            var description =
                measureFieldExpression is not null && measurePrimaryFieldDependencies.OnlyOrDefault() is { } f
                    ? $"<expression relating to {f.Name}>"
                    : identifier ?? STOP_GAP_EXPRESSION;


            //STOPGAP measure: We should decide how to communicate these expressions externally
            return new FilterInfoUnknown(description, GetQuestionClassIds(measureEntityCombination));
        }

        private static string[] GetQuestionClassIds(IEnumerable<EntityType> entityCombination) => entityCombination.Select(type => type.Identifier).ToArray();
    }
}
