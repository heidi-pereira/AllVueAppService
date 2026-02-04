using System.Collections.Generic;
using DashboardMetadataBuilder.MapProcessing.Schema.Sheets;

namespace BrandVueBuilder
{
    internal class Entity
    {
        public string Type { get; }
        public string Identifier { get; }
        public IReadOnlyCollection<EntityInstance> Instances { get; }
        public string IdColumn => $"{Identifier}Id";

        public Entity(string type, string identifier, IReadOnlyCollection<EntityInstance> instances)
        {
            Type = type;
            Identifier = identifier;
            Instances = instances;
        }

        public Entity(string type, IReadOnlyCollection<EntityInstance> instances) : this(type, type, instances)
        {
        }

        protected bool Equals(Entity other)
        {
            return string.Equals(Identifier, other.Identifier);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Entity) obj);
        }

        public override int GetHashCode()
        {
            return (Identifier != null ? Identifier.GetHashCode() : 0);
        }
    }
}
