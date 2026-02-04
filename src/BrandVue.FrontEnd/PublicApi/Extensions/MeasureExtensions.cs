using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;

namespace BrandVue.PublicApi.Extensions
{
    public static class MeasureExtensions
    {
        public static string GetMeasureType(this Measure measure)
        {
            string measureType = string.Join("|", measure.EntityCombination.ToOrderedEntityNames());
            return string.IsNullOrWhiteSpace(measureType) ? EntityType.Profile : measureType;
        }

        public static string[] GetMeasureClasses(this Measure measure) =>
            measure.EntityCombination.ToOrderedEntityNames().ToArray();
    }
}
