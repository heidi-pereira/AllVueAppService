namespace BrandVue.SourceData.Models
{
    public class EntityMeanMap
    {
        public string EntityTypeIdentifier { get; set; }
        public IEnumerable<EntityMeanMapping> Mapping { get; set; }

        public EntityMeanMap(string entityTypeIdentifier, IEnumerable<EntityMeanMapping> mapping)
        {
            EntityTypeIdentifier = entityTypeIdentifier;
            Mapping = mapping;
        }
    }

    public class EntityMeanMapping
    {
        public int EntityId { get; set; }
        public string EntityInstanceName { get; set; }
        public int MeanCalculationValue { get; set; }
        public bool IncludeInCalculation { get; set; }

        public EntityMeanMapping(int id, int value, bool includeInCalculation)
        {
            EntityId = id;
            MeanCalculationValue = value;
            IncludeInCalculation = includeInCalculation;
        }
    }
}
