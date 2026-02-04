using BrandVue.EntityFramework;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;
using Newtonsoft.Json;

namespace BrandVue.Services.Llm.Discovery
{
    public class MetadataStructureProvider : IMetadataStructureProvider
    {
        private readonly IMeasureRepository _measureRepository;
        private readonly ISubsetRepository _subsetRepository;
        private readonly IPageHierarchyGenerator _pageHierarchyGenerator;

        public MetadataStructureProvider(
            IMeasureRepository measureRepository,
            ISubsetRepository subsetRepository,
            IPageHierarchyGenerator pageHierarchyGenerator
            )
        {
            _measureRepository = measureRepository;
            _subsetRepository = subsetRepository;
            _pageHierarchyGenerator = pageHierarchyGenerator;
        }

        public IEnumerable<PageDescriptor> GetPages(string subsetId)
        {
            var pages = _pageHierarchyGenerator.GetHierarchy(_subsetRepository.Get(subsetId));
            return FlattenHeirarchy(pages).ToList();
        }

        public IEnumerable<PageDescriptor> GetPages(string subsetId, string[] metricNames)
        {
            // we need to fix this so it's not looking for exact matches
            bool ReferencesMetric(PageDescriptor page)
            {
                if (metricNames.Any(a=> page.Name.Contains(a)))
                    return true;

                if (page.Panes is not null)
                {
                    if (page.Panes.Any(a => 
                            (a.Spec is not null && metricNames.Any(b => a.Spec.Contains(b)))
                            || (a.Spec2 is not null && metricNames.Any(b => a.Spec2.Contains(b)))
                        ))
                        return true;

                    if (page.Panes
                        .Where(a=>a.Parts is not null)
                        .Any(a1 => a1.Parts.Any(a =>
                            (a.Spec1 is not null && metricNames.Any(b => a.Spec1.Contains(b)))
                            || (a.Spec2 is not null && metricNames.Any(b => a.Spec2.Contains(b)))
                            || (a.Spec3 is not null && metricNames.Any(b => a.Spec3.Contains(b))))))
                        return true;
                }
                return false;
            }

            var pages = _pageHierarchyGenerator.GetHierarchy(_subsetRepository.Get(subsetId));
            return FlattenHeirarchy(pages).Where(ReferencesMetric).ToList();
        }

        public IEnumerable<PageDescriptorAndReferencedMetrics> GetPagesAndReferencedMetrics(string subsetId, string[] metricNames = null)
        {
            HashSet<string> ReferencedMetrics(PageDescriptor page)
            {
                HashSet<string> res = new();
                res.AddRange(metricNames.Where(mn => page.Name.Contains(mn)));
                res.AddRange(metricNames.Where(mn => page.Panes is not null && page.Panes.Any(a =>
                    (a.Spec is not null && a.Spec.Contains(mn))
                    || (a.Spec2 is not null && a.Spec2.Contains(mn)))));
                res.AddRange(metricNames.Where(mn =>
                    page.Panes is not null
                    && page.Panes
                        .Where(a => a.Parts is not null)
                        .Any(a1 => a1.Parts.Any(a =>
                            (a.Spec1 is not null && a.Spec1.Contains(mn))
                            || (a.Spec2 is not null && a.Spec2.Contains(mn))
                            || (a.Spec3 is not null && a.Spec3.Contains(mn))))));
                return res;
            }

            if (metricNames is null)
                metricNames = _measureRepository.GetAllMeasuresIncludingDisabledForSubset(_subsetRepository.Get(subsetId))
                    .Where(a => !a.Disabled)
                    .Select(s => s.Name)
                    .ToArray();

            var pages = _pageHierarchyGenerator.GetHierarchy(_subsetRepository.Get(subsetId));
            var flatPages = FlattenHeirarchy(pages)
                .Select(s => new PageDescriptorAndReferencedMetrics(s, ReferencedMetrics(s)));

            return flatPages.Where(a=>a.MetricNames is not null && a.MetricNames.Count > 0);
        }
         
        public IEnumerable<Measure> GetMeasures(string subsetId)
        {
            var measures = _measureRepository.GetAllMeasuresIncludingDisabledForSubset(_subsetRepository.Get(subsetId));
            return measures.Where(a => !a.Disabled);
        }

        public ICollection<FilterDescriptor> GetFilters(IEnumerable<int> pageId, IEnumerable<int> metricIds)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<PageDescriptor> FlattenHeirarchy(IEnumerable<PageDescriptor> pages)
        {
            foreach (PageDescriptor page in pages)
            {
                yield return page;

                if (page.ChildPages != null)
                {
                    foreach (PageDescriptor childPage in FlattenHeirarchy(page.ChildPages))
                    {
                        yield return childPage;
                    }
                }
            }
        }

    }

    public record PageDescriptorAndReferencedMetrics(PageDescriptor PageDescriptor, HashSet<string> MetricNames);

}