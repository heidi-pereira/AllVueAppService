namespace Vue.Common.Auth.Permissions;

public interface IRole
{
    int Id { get; }
    string RoleName { get; }
    string Organisation { get; }
    IReadOnlyCollection<IPermissionFeatureOption> Permissions { get; }
}