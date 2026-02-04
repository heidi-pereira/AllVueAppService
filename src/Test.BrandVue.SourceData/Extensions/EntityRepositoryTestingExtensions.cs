using BrandVue.SourceData.Entity;

namespace Test.BrandVue.SourceData.Extensions
{
    public static class EntityRepositoryTestingExtensions
    {
        public static void AddInstances(this EntityInstanceRepository entityInstanceRepository, string entityType, params EntityInstance[] instances)
        {
            var instanceRepository = new MapFileEntityInstanceRepository();
            foreach (var entityInstance in instances)
            {
                var insertedInstance = instanceRepository.GetOrCreate(entityInstance.Id);
                insertedInstance.Name = entityInstance.Name;
                insertedInstance.Subsets = entityInstance.Subsets;
            }

            entityInstanceRepository.AddForEntityType(entityType, instanceRepository);
        }
    }
}