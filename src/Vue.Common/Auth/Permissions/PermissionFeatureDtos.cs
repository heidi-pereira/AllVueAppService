namespace Vue.Common.Auth.Permissions;

public record PermissionFeatureOptionDto (int Id, string Name) : IPermissionFeatureOption;
public record PermissionFeatureDto (int Id, string Name, string SystemKey, List<IPermissionFeatureOption> Options) : IPermissionFeature
{
    public string SystemKey { get; init; } = SystemKey;
    public List<IPermissionFeatureOption> Options { get; init; } = Options ?? new List<IPermissionFeatureOption>();
}