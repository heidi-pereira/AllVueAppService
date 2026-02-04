using BrandVue.PublicApi.Models;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;

namespace BrandVue.Services
{
    /// <summary>
    /// Abstract the measures the user is allowed to see versus what's in the measure repo. 
    /// </summary>
    public interface IClaimRestrictedMetricRepository
    {
        /// <remarks>
        /// Non-lazy so that it's still in the request context and can access the user claims
        /// </remarks>
        IReadOnlyCollection<Measure> GetAllowed(Subset subset);

        IReadOnlyCollection<Measure> GetAllowed(Subset subset, IEnumerable<ClassDescriptor> classes);
    }
}