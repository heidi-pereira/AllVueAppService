using AuthServer.GeneratedAuthApi;
using Vue.Common.AuthApi.Models;

namespace Vue.Common.AuthApi
{
    public interface IExtendedAuthApiClient
    {
        Task<CompanyNode?> GetCompanyTree(string shortCode, CancellationToken cancellationToken);
        Task<IEnumerable<CompanyNode>> GetCompanyAndChildrenList(string shortCode, CancellationToken cancellationToken);
        Task<IList<CompanyModel>> GetCompanyAncestorsById(string authCompanyId, bool includeSavanta, CancellationToken cancellationToken);
        public Task<CompanyNode?> GetCompanyById(string id, CancellationToken token);
        public Task<CompanyNode?> GetCompanyByShortCode(string shortCode, CancellationToken token);
    }
}