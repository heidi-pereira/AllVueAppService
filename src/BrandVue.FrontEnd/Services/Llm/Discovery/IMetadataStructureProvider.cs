using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Measures;
using JetBrains.Annotations;

namespace BrandVue.Services.Llm.Discovery
{
    public interface IMetadataStructureProvider
    {
        IEnumerable<Measure> GetMeasures(string subsetId);
        IEnumerable<PageDescriptor> GetPages(string subsetId);
        IEnumerable<PageDescriptor> GetPages(string subsetId, string[] metricNames);
        IEnumerable<PageDescriptorAndReferencedMetrics> GetPagesAndReferencedMetrics(string subsetId, string[] metricNames = null);
        ICollection<FilterDescriptor> GetFilters([CanBeNull]IEnumerable<int> pageId, [CanBeNull]IEnumerable<int> metricIds);
    }
}