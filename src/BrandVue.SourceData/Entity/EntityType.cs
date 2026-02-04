using BrandVue.EntityFramework.MetaData;
using Newtonsoft.Json;
using NJsonSchema.Annotations;

namespace BrandVue.SourceData.Entity
{
    /// <summary>
    /// An entity represents a discrete categorical dimension.
    /// e.g. Brand, Product, ReasonForChoosing
    /// </summary>
    public sealed class EntityType : IEquatable<EntityType>, IComparable<EntityType>, IComparable
    {
        /// <summary>
        /// Special case with no instances, occasionally used to mean "no entity type".
        /// We should generally move away from this where possible and use an empty EntityCombination
        /// </summary>
        public const string Profile = "profile";
        public const string Brand = "brand";
        public const string Product = "product";
        public const string Region = "region";
        public const string City = "city";
        public const string GenericQuestion = "genericQuestion";

        public EntityType()
        {
        }

        public EntityType(string identifier, string displayNameSingular, string displayNamePlural)
        {
            Identifier = identifier;
            DisplayNameSingular = displayNameSingular;
            DisplayNamePlural = displayNamePlural;
        }

        public string Identifier { get; set; }
        public string DisplayNameSingular { get; set; }
        public string DisplayNamePlural { get; set; }
        public bool IsProfile => NameEquals(Profile);
        public bool IsBrand => NameEquals(Brand);

        [JsonIgnore]
        public bool IsProduct => NameEquals(Product);

        [JsonIgnore]
        public HashSet<string> SurveyChoiceSetNames { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static EntityType ProfileType => 
            new(Profile, "Profile", "Profiles") {CreatedFrom = EntityTypeCreatedFrom.Default};

        public bool Equals(EntityType other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return NameEquals(other.Identifier);
        }

        private bool NameEquals(string name)
        {
            return string.Equals(Identifier, name, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is EntityType other && Equals(other);

        public static bool operator ==(EntityType id1, EntityType id2)
        {
            if (id1 is null)
            {
                return id2 is null;
            }

            return id1.Equals(id2);
        }

        public static bool operator !=(EntityType id1, EntityType id2) => !(id1 == id2);

        public override int GetHashCode() => Identifier.GetHashCode(StringComparison.OrdinalIgnoreCase);

        public override string ToString() => $"{GetType().Name}: {Identifier}";

        public int CompareTo(EntityType other) =>
            ReferenceEquals(this, other) ? 0 :
            ReferenceEquals(null, other) ? 1 :
            string.Compare(Identifier, other.Identifier, StringComparison.OrdinalIgnoreCase);

        public int CompareTo(object obj) =>
            ReferenceEquals(null, obj) ? 1 :
            ReferenceEquals(this, obj) ? 0 :
            obj is EntityType other ? CompareTo(other) :
            throw new ArgumentException($"Object must be of type {nameof(EntityType)}");
        
        [CanBeNull]
        public EntityTypeCreatedFrom? CreatedFrom { get; set; }
    }
}
