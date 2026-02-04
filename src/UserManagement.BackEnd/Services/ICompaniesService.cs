using UserManagement.BackEnd.Models;

namespace UserManagement.BackEnd.Services
{
    public interface ICompaniesService
    {
        Task<List<string>> GetCompanyAncestorNames(string companyId, Func<string, bool> filterByCompanySecurityGroup, CancellationToken token);
        public Task<CompanyWithProducts> GetCompanyWithProductsById(string companyId, Func<string, bool> filterByCompanySecurityGroup, CancellationToken token);
        public Task<CompanyWithProducts> GetCompanyWithProductsByShortCode(string shortCode, Func<string, bool> filterByCompanySecurityGroup, CancellationToken token);
        public Task<CompanyWithProducts> GetCompanyWithProductsAndChildCompanies(string shortCode, Func<string, bool> filterByCompanySecurityGroup, CancellationToken token);
    }
}
