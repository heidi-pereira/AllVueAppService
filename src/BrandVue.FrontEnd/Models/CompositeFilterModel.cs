using NJsonSchema.Annotations;

namespace BrandVue.Models
{
    public class CompositeFilterModel
    {
        [CanBeNull] public string Name { get; set; } // init setter when C#9
        public FilterOperator FilterOperator { get; }
        public IReadOnlyCollection<MeasureFilterRequestModel> Filters { get; }
        public IReadOnlyCollection<CompositeFilterModel> CompositeFilters { get; }

        public CompositeFilterModel(FilterOperator filterOperator = FilterOperator.And, IEnumerable<MeasureFilterRequestModel> filters = null, IEnumerable<CompositeFilterModel> compositeFilters = null)
        {
            FilterOperator = filterOperator;
            Filters = filters?.ToArray() ?? Array.Empty<MeasureFilterRequestModel>();
            CompositeFilters = compositeFilters?.ToArray() ?? Array.Empty<CompositeFilterModel>();
        }
    }

    public enum FilterOperator
    {
        And,
        Or
    }
}