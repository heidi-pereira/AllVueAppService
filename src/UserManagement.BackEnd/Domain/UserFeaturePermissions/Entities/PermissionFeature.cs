using BrandVue.EntityFramework.MetaData.Authorisation;

namespace UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities;

public class PermissionFeature
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public SystemKey SystemKey { get; private set; }

    private readonly List<PermissionOption> _options = new();
    public IReadOnlyCollection<PermissionOption> Options => _options.AsReadOnly();

    public PermissionFeature(string name, SystemKey systemKey)
    {
        Name = name;
        SystemKey = systemKey;
    }

    public PermissionFeature(int id, string name, SystemKey systemKey)
    {
        Id = id;
        Name = name;
        SystemKey = systemKey;
    }

    public void AddOption(PermissionOption option)
    {
        if (!_options.Contains(option))
            _options.Add(option);
    }
}