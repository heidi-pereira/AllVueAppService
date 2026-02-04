namespace Vue.Common.Auth.Permissions
{
    public interface IPermissionFeatureOptionWithCode
    {
        int Id { get; }
        string Name { get; }
        PermissionFeaturesOptions Code { get; }
    }
}