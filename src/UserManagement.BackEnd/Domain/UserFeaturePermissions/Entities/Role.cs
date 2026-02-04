namespace UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities;

public class Role
{
    public const int MaxRoleNameLength = 35;

    public int Id { get; private set; }
    public string RoleName { get; private set; }
    public string OrganisationId { get; private set; }
    public string UpdatedByUserId { get; private set; }
    public DateTime UpdatedDate { get; private set; }

    private readonly List<PermissionOption> _options = new();
    public IReadOnlyCollection<PermissionOption> Options => _options.AsReadOnly();

    public Role(string roleName, string organisation, string updatedByUserId)
    {
        RoleName = roleName;
        OrganisationId = organisation;
        UpdatedByUserId = updatedByUserId;
        UpdatedDate = DateTime.UtcNow;
    }

    public Role(int id, string roleName, string organisation, string updatedByUserId)
    {
        Id = id;
        RoleName = roleName;
        OrganisationId = organisation;
        UpdatedByUserId = updatedByUserId;
        UpdatedDate = DateTime.UtcNow;
    }

    public void AssignPermission(PermissionOption option)
    {
        if (!_options.Contains(option))
            _options.Add(option);
    }

    public void SetId(int id)
    {
        Id = id;
    }

    public void Update(string name, string updatedByUserId, IEnumerable<PermissionOption> newOptions)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxRoleNameLength)
            throw new ArgumentException($"Role name must be between 1 and {MaxRoleNameLength} characters.");

        RoleName = name;
        UpdatedByUserId = updatedByUserId;
        UpdatedDate = DateTime.UtcNow;

        _options.Clear();
        foreach (var option in newOptions)
        {
            AssignPermission(option);
        }
    }
}