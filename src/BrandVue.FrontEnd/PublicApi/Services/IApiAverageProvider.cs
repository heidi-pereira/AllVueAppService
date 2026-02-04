using BrandVue.PublicApi.Models;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Subsets;

namespace BrandVue.PublicApi.Services
{
    public interface IApiAverageProvider
    {
        IEnumerable<AverageDescriptor> GetAllAvailableAverageDescriptors(Subset subset);
        IEnumerable<AverageDescriptor> GetSupportedAverageDescriptorsForWeightings(Subset surveyset);
    }
}
