using BrandVue.EntityFramework.Answers;

namespace BrandVue.SourceData.Respondents;

public class EntityFieldDefinitionModel : IEquatable<EntityFieldDefinitionModel>
{
    public DbLocation DbLocation { get; set; }
    public EntityType EntityType { get; }
    public string SafeSqlEntityIdentifier { get; }

    public EntityFieldDefinitionModel(string columnName, EntityType entityType, string entityIdentifier) :
        this(new DbLocation(columnName), entityType, entityIdentifier)
    {
    }

    public EntityFieldDefinitionModel(DbLocation dbLocation, EntityType entityType, string entityIdentifier)
    {
        DbLocation = dbLocation;
        EntityType = entityType;
        SafeSqlEntityIdentifier = entityIdentifier.AssertSafeQuotedSqlId();
    }

    public bool Equals(EntityFieldDefinitionModel other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(DbLocation, other.DbLocation) && Equals(EntityType, other.EntityType) && SafeSqlEntityIdentifier == other.SafeSqlEntityIdentifier;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((EntityFieldDefinitionModel) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = (DbLocation != null ? DbLocation.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (EntityType != null ? EntityType.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (SafeSqlEntityIdentifier != null ? SafeSqlEntityIdentifier.GetHashCode() : 0);
            return hashCode;
        }
    }

    public static bool operator ==(EntityFieldDefinitionModel left, EntityFieldDefinitionModel right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(EntityFieldDefinitionModel left, EntityFieldDefinitionModel right)
    {
        return !Equals(left, right);
    }
}