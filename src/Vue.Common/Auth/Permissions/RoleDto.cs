namespace Vue.Common.Auth.Permissions;

public record RoleDto(
    int Id,
    string RoleName,
    string Organisation,
    IReadOnlyCollection<IPermissionFeatureOption> Permissions
) : IRole
{
    public int Id { get; init; } = Id;
    public string RoleName { get; init; } = RoleName;
    public string Organisation { get; init; } = Organisation;
    public IReadOnlyCollection<IPermissionFeatureOption> Permissions { get; init; } = Permissions;
};