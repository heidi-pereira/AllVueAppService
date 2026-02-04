using Vue.Common.Auth.Permissions;

namespace Vue.Common.Auth
{
    public interface IUserDataPermissionsService
    {
        DataPermissionDto? GetDataPermission();
        Task<DataPermissionDto?> GetDataPermissionAsync();
    }
}
