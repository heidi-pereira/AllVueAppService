using Newtonsoft.Json;

namespace BrandVue.SourceData.Entity
{

    /// <summary>
    /// An entity represents a discrete categorical dimension such as Brand, Product, ReasonForChoosing.
    /// An instance is one particular example within that dimension.
    /// They often correspond directly to a choice in a survey, but can also be created within a Variable.
    /// e.g. For entity type Brand, the instances might be Tesco, Asda, ...
    /// e.g. For entity type Product the instances might be Shampoo, Conditioner,...
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)] // These get serialized a lot, but we only need the id (from base class) and name
    public class EntityInstance : BaseIdentifiableWithUntypedFields, IEquatable<EntityInstance>
    {
        [JsonIgnore]
        public string Identifier { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("color")]
        public string DefaultColor { get; set; }

        public IReadOnlyList<Subset> Subsets { get; set; } = [];

        [JsonProperty("enabledBySubset")]
        public Dictionary<string, bool> EnabledBySubset { get; set; } = new Dictionary<string, bool>();

        [JsonProperty("startDateBySubset")]
        public Dictionary<string, DateTimeOffset> StartDateBySubset { get; set; } = new Dictionary<string, DateTimeOffset>();

        public bool EnabledForSubset(string subsetId) => EnabledBySubset.GetValueOrDefault(subsetId, true);

        public DateTimeOffset? StartDateForSubset(string subsetId) => StartDateBySubset.TryGetValue(subsetId, out var startDate) ? startDate : null;

        [JsonProperty("imageUrl")]
        public string ImageURL { get; set; }

        public bool Equals(EntityInstance other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(other, null)) return false;
            return Name == other.Name && Id == other.Id;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject($@"Entity: {Id} - {Name}");
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as EntityInstance);
        }

        public static bool operator ==(EntityInstance a, EntityInstance b)
        {
            if (a is null)
            {
                return b is null;
            }
            return a.Equals(b);
        }

        public static bool operator !=(EntityInstance a, EntityInstance b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return (Id, Identifier).GetHashCode();
        }

        public class ExactlyEquivalentEqualityComparer : IEqualityComparer<EntityInstance>
        {
            public static ExactlyEquivalentEqualityComparer Instance { get; } = new();
            private ExactlyEquivalentEqualityComparer()
            {
            }

            public bool Equals(EntityInstance x, EntityInstance y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x is null) return false;
                if (y is null) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Id == y.Id && x.Identifier == y.Identifier && x.Name == y.Name && x.DefaultColor == y.DefaultColor && x.Subsets.IsEquivalent(y.Subsets) && x.EnabledBySubset.IsEquivalent(y.EnabledBySubset) && x.StartDateBySubset.IsEquivalent(y.StartDateBySubset);
            }

            /// <summary>
            /// Mostly these should be enough to decide it's worth doing a full equality check - no point trying to take a hash code of various dictionaries since it'll be slow anyway
            /// </summary>
            public int GetHashCode(EntityInstance obj) =>
                HashCode.Combine(obj.Id, obj.Identifier, obj.Name, obj.DefaultColor);
        }
    }
}