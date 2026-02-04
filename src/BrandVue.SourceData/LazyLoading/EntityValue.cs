namespace BrandVue.SourceData.LazyLoading 
{
    /// <summary>
    /// The minimum data to uniquely specify an <see cref="EntityInstance"/> within the containing subset
    /// </summary>
    public class EntityValue : IEquatable<EntityValue>
    {
        public int Value { get; }
        public EntityType EntityType { get; }

        public EntityValue(EntityType entityType, int value)
        {
            Value = value;
            EntityType = entityType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((EntityValue) obj);
        }

        public bool Equals(EntityValue other)
        {
            return Value == other?.Value &&
                   EqualityComparer<EntityType>.Default.Equals(EntityType, other.EntityType);
        }

        public override int GetHashCode()
        {
            return (Value, EntityType).GetHashCode();
        }
    }
}