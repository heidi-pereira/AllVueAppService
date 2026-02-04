using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions.AllVue;

namespace UserManagement.BackEnd.Application.UserDataPermissions.Interfaces
{
    public interface IAllVueRuleRepository
    {
        Task<AllVueRule?> GetDefaultByCompanyAndAllVueProjectAsync(string company,
            ProjectOrProduct projectId, 
            CancellationToken token);

        Task<IList<AllVueRule>> GetByCompaniesAsync(string []companies, CancellationToken token);

        Task AddAsync(AllVueRule rule, CancellationToken token);

        Task UpdateAsync(AllVueRule rule, CancellationToken token);

        Task DeleteAsync(int id, CancellationToken token);
        Task<AllVueRule?> GetById(int newRuleId, CancellationToken token);

        Task<IEnumerable<AllVueRule>> GetByCompanyAndProjectId(string companyId, ProjectOrProduct projectId,
            CancellationToken token);
    }
}