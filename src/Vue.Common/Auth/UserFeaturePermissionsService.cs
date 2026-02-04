using Microsoft.Extensions.Logging;
using Vue.Common.Auth.Permissions;

namespace Vue.Common.Auth
{
    public class UserFeaturePermissionsService : IUserFeaturePermissionsService
    {
        private readonly IUserContext _userContext;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<UserFeaturePermissionsService> _logger;

        public UserFeaturePermissionsService(IUserContext userContext, IPermissionService permissionService, ILogger<UserFeaturePermissionsService> logger)
        {
            _userContext = userContext;
            _permissionService = permissionService;
            _logger = logger;
        }

        private Task<IReadOnlyCollection<PermissionFeatureOptionWithCode>>? _permissionsTask;
        private readonly object _permissionsLock = new object();

        public IReadOnlyCollection<PermissionFeatureOptionWithCode> FeaturePermissions
        {
            get
            {
                return GetFeaturePermissionsAsync().GetAwaiter().GetResult();
            }
        }

        public async Task<IReadOnlyCollection<PermissionFeatureOptionWithCode>> GetFeaturePermissionsAsync()
        {
            if (string.IsNullOrEmpty(_userContext.UserId))
            {
                return [];
            }

            Task<IReadOnlyCollection<PermissionFeatureOptionWithCode>> taskToAwait;

            lock (_permissionsLock)
            {
                if (_permissionsTask == null)
                {
                    _permissionsTask = LoadPermissionsAsync();
                }
                taskToAwait = _permissionsTask;
            }

            try
            {
                return await taskToAwait;
            }
            catch
            {
                lock (_permissionsLock)
                {
                    _permissionsTask = null;
                }
                return [];
            }
        }

        private async Task<IReadOnlyCollection<PermissionFeatureOptionWithCode>> LoadPermissionsAsync()
        {
            try
            {
                var permissions = await _permissionService.GetAllUserFeaturePermissionsAsync(_userContext.UserId, _userContext.Role).ConfigureAwait(false);
                return permissions?.Select(p => new PermissionFeatureOptionWithCode(p.Id, p.Name, (PermissionFeaturesOptions)p.Id)).ToList() ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Permissions {UserId} {Role}", _userContext.UserId, _userContext.Role);
                return [];
            }
        }
    }
}
