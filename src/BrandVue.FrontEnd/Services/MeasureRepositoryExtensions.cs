using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Measures;

namespace BrandVue.Services;

public static class MeasureRepositoryExtensions
{
    public static bool RequiresLegacyBreakCalculation(this IMeasureRepository measureRepository, AppSettings appSettings, CrossMeasure[] crossMeasures)
    {
        if(crossMeasures == null)
        {
            return true;
        }

        var allBreakMeasureNames = crossMeasures.SelectMany(cm => cm.FollowMany(c => c.ChildMeasures));
        var allBreakMeasures = allBreakMeasureNames.Select(cm => measureRepository.Get(cm.MeasureName));
        return allBreakMeasures.Any(MeasureRequiresLegacyBreakCalculation);

        bool MeasureRequiresLegacyBreakCalculation(Measure measure) => !appSettings.UseOptimisedCrossbreakCalculations || FilterValueMappingVariableParser.BreakMeasureRequiresLegacyCalculation(measure);
    }
}