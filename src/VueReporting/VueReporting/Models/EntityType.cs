using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VueReporting.Models
{
    public class EntityType
    {
        public static readonly EntityType Profile
            = new EntityType("profile", "Profile", "Profiles");

        public static readonly EntityType Brand
            = new EntityType("brand", "Brand", "Brands");

        public static readonly EntityType Product
            = new EntityType("product", "Product", "Products");

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
        public bool IsProfile => Equals(Profile);
        public bool IsBrand => Equals(Brand);

        [JsonIgnore]
        public bool IsProduct => Equals(Product);

        public override bool Equals(object obj)
        {
            var other = obj as EntityType;
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return (GetType() == other.GetType()
                    && string.Compare(Identifier, other.Identifier) == 0);
        }

        public static bool operator ==(EntityType id1, EntityType id2)
        {
            if (id1 is null)
            {
                return id2 is null;
            }

            return id1.Equals(id2);
        }

        public static bool operator !=(EntityType id1, EntityType id2)
        {
            return !(id1 == id2);
        }

        public override int GetHashCode()
        {
            return Identifier.GetHashCode();
        }

        public override string ToString()
        {
            return $"{GetType().Name}: {Identifier}";
        }
    }
}
