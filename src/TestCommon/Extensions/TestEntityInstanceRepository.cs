using System.Linq;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;

namespace TestCommon.Extensions
{
    public class TestEntityInstanceRepository : EntityInstanceRepository
    {
        public TestEntityInstanceRepository(params EntityValue[] entityValues)
        {
            foreach (var entityValuesForType in entityValues.GroupBy(e => e.EntityType.Identifier))
            {
                var entityInstanceRepository = new MapFileEntityInstanceRepository();
                foreach (var entityValue in entityValuesForType)
                {
                    entityInstanceRepository.GetOrCreate(entityValue.Value).Name = entityValue.Value.ToString();
                }
                AddForEntityType(entityValuesForType.Key, entityInstanceRepository);
            }
        }
    }
}