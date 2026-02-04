using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Authorisation;
using BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions;
using BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions.AllVue;
using Microsoft.EntityFrameworkCore;
using UserManagement.BackEnd.Application.UserDataPermissions.Interfaces;

namespace UserManagement.BackEnd.Infrastructure.Repositories.UserDataPermissions;

public class UserDataPermissionRepository : IUserDataPermissionRepository
{
    private readonly MetaDataContext _context;

    public UserDataPermissionRepository(MetaDataContext context)
    {
        _context = context;
    }

    public async Task<UserDataPermission?> GetByIdAsync(int userDataPermissionsId, CancellationToken token)=>
        await _context.Set<UserDataPermission>()
            .Include(p => p.Rule)
            .Include(p => ((AllVueRule)p.Rule).Filters)
            .SingleOrDefaultAsync(p => p.Id == userDataPermissionsId,token);


    public async Task<IList<UserDataPermission>> GetByUserIdAsync(string userId, CancellationToken token) =>
        await _context.Set<UserDataPermission>()
            .Include(p => p.Rule)
            .Include(p => ((AllVueRule)p.Rule).Filters)
            .Where(p => p.UserId == userId).ToListAsync(token);

    public async Task DeleteAllPermissionsForUserAsync(string userId, CancellationToken token)
    {
        var permissions = await _context.Set<UserDataPermission>()
            .Where(p => p.UserId == userId)
            .ToListAsync(token);
        _context.Set<UserDataPermission>().RemoveRange(permissions);
        await _context.SaveChangesAsync(token);
    }

    public async Task<IList<UserDataPermission>> GetByUserIdsAsync(string[] userIds, CancellationToken token)
    {
        if (userIds == null || userIds.Length == 0)
            return new List<UserDataPermission>();

        return await _context.Set<UserDataPermission>()
            .Include(p => p.Rule)
            .Include(p => ((AllVueRule)p.Rule).Filters)
            .Where(p => userIds.Contains(p.UserId))
            .ToListAsync(token);
    }

    public async Task<UserDataPermission> GetByUserIdByCompanyAndProjectAsync(string userId, string company, ProjectOrProduct projectId, CancellationToken token)
    {
        var res = await _context.Set<UserDataPermission>()
            .Include(p => p.Rule)
            .Include(p => ((AllVueRule)p.Rule).Filters)
            .SingleOrDefaultAsync(p => p.UserId == userId && p.Rule.SystemKey == SystemKey.AllVue && p.Rule is AllVueRule &&
                                       ((AllVueRule)p.Rule).Organisation == company &&
                                       ((AllVueRule)p.Rule).ProjectType == projectId.ProjectType && 
                                       ((AllVueRule)p.Rule).ProjectOrProductId == projectId.ProjectId, token);
        return res;
    }

    public async Task<IEnumerable<UserDataPermission>> GetByCompanyAndAllVueProjectAsync(string company, ProjectOrProduct productId, CancellationToken token)
    {
        var res = await _context.Set<UserDataPermission>()
            .Include(p => p.Rule)
            .Include(p => ((AllVueRule)p.Rule).Filters)
            .Where(p => p.Rule.SystemKey ==SystemKey.AllVue && p.Rule is AllVueRule &&
                               ((AllVueRule)p.Rule).Organisation == company &&
                               ((AllVueRule)p.Rule).ProjectType == productId.ProjectType && 
                               ((AllVueRule)p.Rule).ProjectOrProductId == productId.ProjectId)
            .ToListAsync(token);
        return res;
    }
    public async Task<IEnumerable<UserDataPermission>> GetByCompaniesAndAllVueProjectsAsync(string[] companies, ProjectOrProduct project, CancellationToken token)
    {
        var res = await _context.Set<UserDataPermission>()
            .Include(p => p.Rule)
            .Include(p => ((AllVueRule)p.Rule).Filters)
            .Where(p => p.Rule.SystemKey == SystemKey.AllVue && p.Rule is AllVueRule &&
                        companies.Contains(((AllVueRule)p.Rule).Organisation) &&
                        ((AllVueRule)p.Rule).ProjectType == project.ProjectType &&
                        ((AllVueRule)p.Rule).ProjectOrProductId == project.ProjectId)
            .ToListAsync(token);
        return res;
    }

    public async Task<IEnumerable<UserDataPermission>> GetByRuleIdAsync(int ruleId, CancellationToken token)
    {
        var res = await _context.Set<UserDataPermission>()
            .Include(p => p.Rule)
            .Include(p => ((AllVueRule)p.Rule).Filters)
            .Where(p => p.Rule.SystemKey == SystemKey.AllVue && p.Rule is AllVueRule &&
                        p.Rule.Id == ruleId)
            .ToListAsync(token);
        return res;
    }

    public async Task<IEnumerable<UserDataPermission>> GetByCompaniesAsync(string[] companies, CancellationToken token)
    {
        var res = await _context.Set<UserDataPermission>()
            .Include(p => p.Rule)
            .Include(p => ((AllVueRule)p.Rule).Filters)
            .Where(p => p.Rule.SystemKey == SystemKey.AllVue && p.Rule is AllVueRule &&
                        companies.Contains(((AllVueRule)p.Rule).Organisation))
            .ToListAsync(token);
        return res;
    }

    public async Task AddAsync(UserDataPermission permission, CancellationToken token)
    {
        await _context.Set<UserDataPermission>().AddAsync(permission,token);
        await _context.SaveChangesAsync(token);
    }

    public async Task UpdateAsync(UserDataPermission permission, CancellationToken token)
    {
        _context.Set<UserDataPermission>().Update(permission);
        await _context.SaveChangesAsync(token);
    }

    public async Task DeleteAsync(int id, CancellationToken token)
    {
        var permission = await _context.Set<UserDataPermission>().FindAsync(id, token);
        if (permission != null)
        {
            _context.Set<UserDataPermission>().Remove(permission);
            await _context.SaveChangesAsync(token);
        }
    }

    public async Task<IList<UserDataPermission>> GetByUserIdsAndProjectAsync(string[] userIds, ProjectOrProduct projectId, CancellationToken token)
    {
        if (userIds == null || userIds.Length == 0)
            return new List<UserDataPermission>();

        return await _context.Set<UserDataPermission>()
            .Include(p => p.Rule)
            .Include(p => ((AllVueRule)p.Rule).Filters)
            .Where(p => userIds.Contains(p.UserId) 
                        && p.Rule is AllVueRule
                        && ((AllVueRule)p.Rule).ProjectType == projectId.ProjectType 
                        && ((AllVueRule)p.Rule).ProjectOrProductId == projectId.ProjectId)
            .ToListAsync(token);
    }

    public async Task<IList<UserDataPermission>> GetByRuleId(int ruleId, CancellationToken token)
    {
        return await _context.Set<UserDataPermission>()
            .Include(p => p.Rule)
            .Include(p => ((AllVueRule)p.Rule).Filters)
            .Where(p => p.Rule is AllVueRule
                        && p.Rule.Id == ruleId)
            .ToListAsync(token);
    }
}