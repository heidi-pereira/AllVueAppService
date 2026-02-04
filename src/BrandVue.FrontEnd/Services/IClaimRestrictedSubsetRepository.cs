using BrandVue.SourceData.Subsets;

namespace BrandVue.Services
{
    /// <summary>
    /// Abstract the subsets the user is allowed to see versus what's in the subset repo. 
    /// </summary>
    public interface IClaimRestrictedSubsetRepository
    {
        /// <remarks>
        /// Non-lazy so that it's still in the request context and can access the user claims
        /// </remarks>
        IReadOnlyCollection<Subset> GetAllowed();
    }
}
