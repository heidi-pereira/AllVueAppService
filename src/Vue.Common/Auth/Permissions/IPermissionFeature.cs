namespace Vue.Common.Auth.Permissions;

public interface IPermissionFeature
{
    int Id { get; }
    string Name { get; }
    string SystemKey { get; }
    List<IPermissionFeatureOption> Options { get; }
}