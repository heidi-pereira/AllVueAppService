using Vue.Common.Auth.Permissions;

namespace Vue.Common.Auth
{
    public interface IUserFeaturePermissionsService
    {
        IReadOnlyCollection<PermissionFeatureOptionWithCode> FeaturePermissions { get; }
        Task<IReadOnlyCollection<PermissionFeatureOptionWithCode>> GetFeaturePermissionsAsync();
    }
}
