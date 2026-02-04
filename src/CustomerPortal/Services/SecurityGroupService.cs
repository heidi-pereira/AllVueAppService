using System.Threading;
using System.Threading.Tasks;
using Vue.Common.Auth;
using Vue.Common.AuthApi;

namespace CustomerPortal.Services
{
    public class SecurityGroupService : ISecurityGroupService
    {
        private readonly IAuthApiClientCustomerPortal _authApiClient;
        private readonly IUserContext _userContext;

        public SecurityGroupService(IAuthApiClientCustomerPortal authApiClient, IUserContext userContext)
        {
            _authApiClient = authApiClient;
            _userContext = userContext;
        }

        public async Task<bool> UserHasSecurityGroupAccessFor(string authCompanyId)
        {
            // Security groups are only applicable to Savanta users
            if (!_userContext.IsAuthorizedSavantaUser)
            {
                return true;
            }

            var authCompany = await _authApiClient.GetCompanyById(authCompanyId, CancellationToken.None);
            if (authCompany == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(authCompany.SecurityGroup))
            {
                return true;
            }

            return _userContext.HasSecurityGroupAccess(authCompany.SecurityGroup);
        }
    }
}
