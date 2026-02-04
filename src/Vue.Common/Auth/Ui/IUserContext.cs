using Vue.Common.Auth.Permissions;

namespace Vue.Common.Auth.Ui
{
    public interface IUserContext : IUserContextBase
    {
        public IReadOnlyCollection<PermissionFeatureOptionWithCode> FeaturePermissions { get; }
    }
}
