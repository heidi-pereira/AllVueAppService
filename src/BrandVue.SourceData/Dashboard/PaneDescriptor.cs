using BrandVue.SourceData.CommonMetadata;

namespace BrandVue.SourceData.Dashboard
{

    /// <remarks>
    /// Warning: "Descriptor" is used by all the PublicApi types, but this is not one of those as you can see from the namespace.
    /// </remarks>
    public class PaneDescriptor : BaseMetadataEntity
    {
        public string Id { get; set; }
        public string PageName { get; set; }
        public int Height { get; set; }
        public string PaneType { get; set; }
        public string Spec { get; set; }
        public string Spec2 { get; set; }
        public int View { get; set; }
        public PartDescriptor[] Parts { get; set; }
    }
}
