namespace Vue.Common.Auth.Permissions
{
    public record PermissionFeatureOptionWithCode(int Id, string Name, PermissionFeaturesOptions Code)  : IPermissionFeatureOptionWithCode;
}