using BrandVue.EntityFramework.MetaData;

namespace UserManagement.BackEnd.Models;

public enum AccessStatus
{
    None = 0,
    AllUsers = 1,
    Mixed = 2,
    Restricted = 3
}

public record ProjectIdentifier(
    ProjectType Type,
    int Id)
{
    public string ToLegacyAuthName(IDictionary<int, string> lookupOfIdsToName)
    {
        return Type.ToLegacyAuthName(Id, lookupOfIdsToName);
    }
    public ProjectOrProduct ToProjectOrProduct()
    {
        return new ProjectOrProduct(Type, Id);
    }

    public string ToLegacyProductShortCode()
    {
        return Type.ToLegacyProductShortCode();
    }
}

public record ProductIdentifier(ProjectType Type, int Id) : ProjectIdentifier(Type,Id)
{

}

public record Product(
    ProductIdentifier ProjectId,
    string Name,
    string URL
);

public record Project(
    ProjectIdentifier ProjectId,
    string Name,
    AccessStatus UserAccess,
    string CompanyId,
    string CompanyName,
    int DataGroupCount,
    bool IsShared,
    string URL);