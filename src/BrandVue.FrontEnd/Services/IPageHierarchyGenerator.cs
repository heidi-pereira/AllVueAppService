using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Subsets;

namespace BrandVue.Services
{
    public interface IPageHierarchyGenerator
    {
        PageDescriptor[] GetHierarchy(params Subset[] selectedSubsets);
    }
}
