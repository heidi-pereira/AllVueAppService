using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Authorisation;
using BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions.AllVue;
using Microsoft.EntityFrameworkCore;
using UserManagement.BackEnd.Application.UserDataPermissions.Interfaces;
namespace UserManagement.BackEnd.Infrastructure.Repositories.UserDataPermissions;

public class AllVueRuleRepository : IAllVueRuleRepository
{
    private readonly MetaDataContext _context;

    public AllVueRuleRepository(MetaDataContext context)
    {
        _context = context;
    }

    public async Task<AllVueRule?> GetDefaultByCompanyAndAllVueProjectAsync(string company, ProjectOrProduct projectId, CancellationToken token) =>
        await _context.Set<AllVueRule>()
            .Include(p => p.Filters)
            .SingleOrDefaultAsync(p => p.SystemKey == SystemKey.AllVue && p.Organisation == company && p.ProjectType == projectId.ProjectType && p.ProjectOrProductId == projectId.ProjectId && p.AllUserAccessForSubProduct,token);


    public async Task<IList<AllVueRule>> GetByCompaniesAsync(string[] companies, CancellationToken token)
    {
        var vals = _context.Set<AllVueRule>()
            .Include(p => p.Filters)
            .Where(p => p.SystemKey == SystemKey.AllVue && companies.Contains(p.Organisation ));
        return await vals.ToListAsync(token);
    }

    public async Task AddAsync(AllVueRule rule, CancellationToken token)
    {
        var dbRules = await GetByCompanyAndProjectId(rule.Organisation, new ProjectOrProduct(rule.ProjectType, rule.ProjectOrProductId), token);
        if (dbRules.Any(x => x.RuleName == rule.RuleName))
        {
            throw new ArgumentException($"A data group for this company and project {rule.ProjectType}-{rule.ProjectOrProductId} already exists.");
        }

        await _context.Set<AllVueRule>().AddAsync(rule, token);
        await _context.SaveChangesAsync(token);
    }

    public async Task UpdateAsync(AllVueRule rule, CancellationToken token)
    {
        var dbRules = await GetByCompanyAndProjectId(rule.Organisation, new ProjectOrProduct(rule.ProjectType, rule.ProjectOrProductId), token);
        if (dbRules.Any(x => x.RuleName == rule.RuleName && x.Id != rule.Id))
        {
            throw new ArgumentException($"A data group for this company and project {rule.ProjectType}-{rule.ProjectOrProductId} already exists.");
        }

        _context.Set<AllVueRule>().Update(rule);
        await _context.SaveChangesAsync(token);
    }

    public async Task DeleteAsync(int id, CancellationToken token)
    {
        var allVueRule = await _context.Set<AllVueRule>().FindAsync(id, token);
        if (allVueRule != null)
        {
            _context.Set<AllVueRule>().Remove(allVueRule);
            await _context.SaveChangesAsync(token);
        }
    }

    public async Task<AllVueRule?> GetById(int ruleId, CancellationToken token)
    {
        return await _context.Set<AllVueRule>()
            .Include(p => p.Filters)
            .SingleOrDefaultAsync(p => p.SystemKey == SystemKey.AllVue && p.Id == ruleId, token);
    }

    public async Task<IEnumerable<AllVueRule>> GetByCompanyAndProjectId(string companyId, ProjectOrProduct projectId, CancellationToken token)
    {
        return await _context.Set<AllVueRule>()
            .Include(p => p.Filters)
            .Where(p => p.Organisation == companyId && p.SystemKey == SystemKey.AllVue && p.ProjectType == projectId.ProjectType && p.ProjectOrProductId == projectId.ProjectId)
            .ToListAsync(token);
    }
}