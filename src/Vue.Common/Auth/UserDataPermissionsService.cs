using BrandVue.EntityFramework;
using Microsoft.Extensions.Logging;
using Vue.Common.Auth.Permissions;

namespace Vue.Common.Auth
{
    public class UserDataPermissionsService : IUserDataPermissionsService
    {
        private IUserContext _userContext;
        private IPermissionService _permissionService;
        private IProductContext? _productContext;
        private readonly ILogger<UserDataPermissionsService> _logger;

        public UserDataPermissionsService(
            IUserContext userContext,
            IPermissionService permissionService,
            IProductContext? productContext,
            ILoggerFactory loggerFactory)
        {
            _userContext = userContext;
            _permissionService = permissionService;
            _productContext = productContext;
            _logger = loggerFactory.CreateLogger<UserDataPermissionsService>();
        }

        public DataPermissionDto? GetDataPermission()
        {
            return GetDataPermissionAsync().GetAwaiter().GetResult();
        }

        private Task<DataPermissionDto?>? _dataPermissionsTask;
        private readonly object _dataPermissionsLock = new object();

        public async Task<DataPermissionDto?> GetDataPermissionAsync()
        {
            if (string.IsNullOrEmpty(_userContext.UserId))
            {
                return null;
            }

            var companyId = _productContext.SurveyAuthCompanyId;
            var productShortCode = _productContext.ShortCode;
            var subProductId = _productContext.SubProductId;

            if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(productShortCode))
            {
                return null;
            }

            Task<DataPermissionDto?> taskToAwait;

            lock (_dataPermissionsLock)
            {
                if (_dataPermissionsTask == null)
                {
                    _dataPermissionsTask = LoadDataPermissionForCompanyAndProjectAsync(companyId, productShortCode, subProductId);
                }
                taskToAwait = _dataPermissionsTask;
            }

            try
            {
                return await taskToAwait;
            }
            catch
            {
                lock (_dataPermissionsLock)
                {
                    _dataPermissionsTask = null;
                }
                return null;
            }
        }

        private async Task<DataPermissionDto?> LoadDataPermissionForCompanyAndProjectAsync(string companyId, string productShortCode, string subProductId)
        {
            try
            {
                return await _permissionService.GetUserDataPermissionForCompanyAndProjectAsync(companyId, productShortCode, subProductId, _userContext.UserId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user's data permission");
                return null;
            }
        }
    }
}
