using System.Collections.Immutable;
using Newtonsoft.Json;

namespace BrandVue.SourceData.Entity
{
    public class TargetInstances : IDataTarget
    {
        public EntityType EntityType { get; }
        public ImmutableArray<EntityInstance> OrderedInstances { get; }
        public ImmutableArray<int> SortedEntityInstanceIds { get; }

        public TargetInstances(EntityType type, IEnumerable<EntityInstance> instances)
        {
            OrderedInstances = instances.OrderBy(x => x.Id).ToImmutableArray();
            if (type.IsProfile && OrderedInstances.Any())
            {
                throw new ArgumentException($"Instances must be empty for {type}");
            }

            if (!type.IsProfile && !OrderedInstances.Any())
            {
                throw new ArgumentException($"Missing entity instances for {type}");
            }

            EntityType = type;
            SortedEntityInstanceIds = OrderedInstances.Select(i => i.Id).ToImmutableArray();
        }

        protected bool Equals(TargetInstances other)
        {
            return Equals(EntityType, other.EntityType) && SortedEntityInstanceIds.SequenceEqual(other.SortedEntityInstanceIds);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TargetInstances) obj);
        }

        public override int GetHashCode()
        {
            return (EntityType, EntityInstanceIds: SortedEntityInstanceIds).GetHashCode();
        }

        internal string GetLoggableInstanceString()
        {
            return JsonConvert.SerializeObject(this.OrderedInstances.Select(instance => instance.Name));
        }
    }
}