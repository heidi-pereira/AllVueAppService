using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;

namespace BrandVue.PublicApi.Definitions
{
    public interface IResponseFieldDescriptorLoader
    {
        IEnumerable<ResponseFieldDescriptor> GetFieldDescriptors(Subset subset, bool includeText = false);

        IEnumerable<ResponseFieldDescriptor> GetFieldDescriptors(Subset subset, IEnumerable<EntityType> entityTypes, bool includeText = false);
    }
}