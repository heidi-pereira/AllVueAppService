using BrandVue.SourceData.CommonMetadata;

namespace BrandVue.SourceData.Entity
{
    public interface IEntitySetRepository
    {
        IReadOnlyCollection<EntitySet> GetAllFor(string entityType, Subset subset, string organisation);
        IReadOnlyCollection<EntitySet> GetOrganisationAgnostic(string entityType, Subset subset);
        /// <summary>
        /// DANGER: Consider using GetOrganisationAgnostic instead
        /// This method must only be used in a non-client-accessible context (e.g. entirely internally or sysadmin only) since it breaks across org boundaries
        /// </summary>
        IReadOnlyCollection<EntitySet> InsecureGetAllForAnyCompany(string entityType, Subset subset);
        EntitySet GetDefaultSetForOrganisation(string entityType, Subset subset, string organisation);
    }
}