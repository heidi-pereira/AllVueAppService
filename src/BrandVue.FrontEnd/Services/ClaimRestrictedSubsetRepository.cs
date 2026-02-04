using Vue.AuthMiddleware;
using BrandVue.Middleware;
using BrandVue.SourceData.Subsets;
using JetBrains.Annotations;
using BrandVue.PublicApi.Extensions;

namespace BrandVue.Services
{
    public class ClaimRestrictedSubsetRepository : IClaimRestrictedSubsetRepository
    {
        private readonly ISubsetRepository _subsets;
        private readonly IUserContext _userContext;
        private readonly RequestScope _requestScope;

        public ClaimRestrictedSubsetRepository(ISubsetRepository subsets, IUserContext userContext, RequestScope requestScope)
        {
            _subsets = subsets;
            _userContext = userContext;
            _requestScope = requestScope;
        }

        private static bool IsAccessibleAndEnabled(Subset s)
        {
            return s.EnableRawDataApiAccess && !s.Disabled;
        }

        private bool HasAccessAndEnabledInRequestScope(Subset s)
        {
            switch (_requestScope.Resource)
            {
                case RequestResource.PublicApi:
                    return IsAccessibleAndEnabled(s);
                default:
                    return !s.Disabled;
            }
        }
        
        public IReadOnlyCollection<Subset> GetAllowed()
        {
            var enabledSubsets = _subsets.Where(HasAccessAndEnabledInRequestScope);
            var subsetIds = _userContext.GetProductSubsets(_requestScope);
            var subsets = subsetIds.Contains(Constants.AllSubsetsForProduct)
                ? enabledSubsets
                : enabledSubsets.Where(s => subsetIds.Contains(s.Id));
            var listOfSubsets = subsets.ToList();
            return listOfSubsets;
        }
    }

    /// <summary>
    /// Now that we are coalescing a null alias onto the Id these methods are safe to use for subsets without an alias such as eating out currently
    /// </summary>
    [UsedImplicitly]
    internal static class ClaimRestrictedSubsetRepositoryExtensions
    {
        public static Subset GetWithAlias(this IClaimRestrictedSubsetRepository repository, string alias)
        {
            return repository.GetAllowed().SingleOrDefault(s => s.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase)) ??
                   throw new ArgumentOutOfRangeException(nameof(alias), alias,
                       $"You do not have permission to view the subset with this {nameof(alias)}");
        }

        public static bool HasSubsetWithAlias(this IClaimRestrictedSubsetRepository repository, string alias)
        {
            return repository.GetAllowed().Count(s => s.Alias != null && s.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase)) == 1;
        }
    }
}