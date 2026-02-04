using BrandVue.SourceData.CommonMetadata;

namespace BrandVue.SourceData.Filters
{
    public enum FilterTypes
    {
        Profile = 1,
        Brand = 2,
        Timing = 3,
    }

    public enum FilterValueTypes
    {
        Category = 1,
        Value = 2,
    }

    public class FilterDescriptor : BaseMetadataEntity
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Field { get; set; }
        public string VariableName{ get; set; }
        public FilterValueTypes FilterValueType { get; set; }
        public string Categories { get; set; }
        public int InternalIndex { get; set; }
        public override string ToString()
        {
            return $"{Name} {(Subset != null && Subset.Any() ? Subset[0].DisplayName : "")}";
        }
    }
}