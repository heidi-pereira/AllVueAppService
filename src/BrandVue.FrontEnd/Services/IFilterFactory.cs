using BrandVue.Models;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;

namespace BrandVue.Services
{
    public interface IFilterFactory
    {
        IFilter CreateFilterForMeasure(CompositeFilterModel filterModel, Measure measureForCalculation, Subset subset);
    }
}