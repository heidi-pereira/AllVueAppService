using System.Diagnostics;
using BrandVue.EntityFramework;
using BrandVue.SourceData.CommonMetadata;

namespace BrandVue.SourceData.Dashboard
{
    /// <remarks>
    /// Warning: "Descriptor" is used by all the PublicApi types, but this is not one of those as you can see from the namespace.
    /// </remarks>
    [DebuggerDisplay("Id: {Id}, Name: {Name}, PageType: {PageType}")]
    public class PageDescriptor : IMetadataEntity, ICloneable, ISubsetIdsProvider<IEnumerable<string>>
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string MenuIcon { get; set; }
        public string PageType { get; set; }
        public string HelpText { get; set; }
        public ICollection<PageSubsetConfigurationModel> PageSubsetConfiguration { get; set; }
        public int MinUserLevel { get; set; }
        public bool StartPage { get; set; }
        public string Layout { get; set; }
        public string PageTitle { get; set; }
        public string [] AverageGroup { get; set; }
        public int? PageDisplayIndex { get; set; }
        public PaneDescriptor[] Panes { get; set; }
        public IList<PageDescriptor> ChildPages { get; set; }
        public string DefaultBase { get; set; }
        public int? DefaultPaneViewType { get; set; }
        public string[] Environment { get; set; }
        public string[] Roles { get; set; }
        public bool Disabled { get; set; }

        public IReadOnlyList<string> GetSubsets()
        {
            return PageSubsetConfiguration.Where(x=>x.Enabled).Select(x => x.Subset).ToList();
        }

        public void SetSubsets(IEnumerable<string> subsets, ISubsetRepository repository)
        {
            PageSubsetConfiguration = subsets.Select(subset =>
                new PageSubsetConfigurationModel(subset: subset, enabled: true)).ToList();
        }

        public object Clone()
        {
            var clone = (PageDescriptor)MemberwiseClone();
            clone.PageSubsetConfiguration = PageSubsetConfiguration.Select(psc => new PageSubsetConfigurationModel(psc.Subset, psc.Enabled, psc.HelpText)).ToList();
            return clone;
        }

        IEnumerable<string> ISubsetIdsProvider<IEnumerable<string>>.SubsetIds => PageSubsetConfiguration.Select(x => x.Subset);
    }
}
