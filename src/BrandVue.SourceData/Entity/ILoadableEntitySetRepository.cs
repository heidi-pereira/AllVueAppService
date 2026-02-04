using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.CommonMetadata;

namespace BrandVue.SourceData.Entity
{
    public interface ILoadableEntitySetRepository : IEntitySetRepository
    {
        void Add(EntitySet entitySet, string entityType, Subset subset);
        void Remove(EntitySet entitySet, string entityType, Subset subset);
        void RemoveAllFromAllOrganisationsForType(EntityType typeToRemove);
    }
}