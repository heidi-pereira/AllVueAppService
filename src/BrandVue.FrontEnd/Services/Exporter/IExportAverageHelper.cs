using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;

namespace BrandVue.Services.Exporter
{
    public interface IExportAverageHelper
    {
        AverageType[] VerifyAverageTypesForMeasure(Measure measure, AverageType[] averageTypes, Subset subset);
    }
}