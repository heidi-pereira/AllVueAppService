using BrandVue.SourceData.CommonMetadata;

namespace BrandVue.SourceData.QuotaCells
{
    /// <summary>
    /// Constructs a <see cref="IProfileResponseAccessor"/> instance for a provided subset context.
    /// It is only intended that one of these exists per loader
    /// </summary>
    public interface IProfileResponseAccessorFactory
    {
        IProfileResponseAccessor GetOrCreate(Subset subset);
    }
}