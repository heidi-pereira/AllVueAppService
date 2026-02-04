using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Utils;

namespace BrandVue.SourceData.Calculation
{
    public class OrFilter : CompositeFilter
    {
        public OrFilter(IReadOnlyCollection<IFilter> filters) : base(filters)
        {
        }

        protected override Func<IProfileResponseEntity, bool> CreateForEntityValues(Func<IProfileResponseEntity, bool>[] subFilters) => p => subFilters.Any(p);
    }
}