using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Vue.Common.Auth.Permissions;

namespace Vue.Common.Auth
{
    public class UserDataPermissionsOrchestrator : IUserDataPermissionsOrchestrator
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserDataPermissionsOrchestrator(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private static T GetService<T>(HttpContext context) => context.RequestServices.GetRequiredService<T>();

        private IUserDataPermissionsService UserPermissionsService => GetService<IUserDataPermissionsService>(_httpContextAccessor.HttpContext);

        public DataPermissionDto? GetDataPermission()
        {
            return UserPermissionsService.GetDataPermission();
        }

        public Task<DataPermissionDto?> GetDataPermissionAsync()
        {
            return UserPermissionsService.GetDataPermissionAsync();
        }

    }
}
