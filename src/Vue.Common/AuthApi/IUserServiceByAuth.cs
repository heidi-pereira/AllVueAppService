using AuthServer.GeneratedAuthApi;

namespace Vue.Common.AuthApi
{
    public interface IUserServiceByAuth
    {
        Task DeleteUser(string shortCode, string requesterEmail, string userId, CancellationToken cancellationToken);
        Task ResendEmail(string shortCode, string userEmail, CancellationToken cancellationToken);
        Task<IEnumerable<UserProjectsModel>> GetAllUserDetailsForCompanyAndChildrenScopeAsync(string shortCode,
            string currentUserId, string requestCompanyId, bool includeSavantaUsers, bool includeProductDetails, CancellationToken cancellationToken);
        Task<UserProjectsModel> GetUserAsync(string shortCode, string userId, string proxyUserId, CancellationToken cancellationToken);
        Task<UserProjectsModel> UpdateUserAsync(string shortCode, string currentUser, UserUpdateDetails user,
            CancellationToken cancellationToken);
        Task<UserProjectsModel> CreateUserAsync(string shortCode, string currentUser, UserAddDetails user,
            CancellationToken cancellationToken);
    }
}
