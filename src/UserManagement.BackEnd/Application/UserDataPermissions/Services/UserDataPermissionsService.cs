using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Authorisation;
using BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions;
using UserManagement.BackEnd.Application.UserDataPermissions.Interfaces;
using UserManagement.BackEnd.Domain.UserDataPermissions.Entities;

using EF = BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions.AllVue;

namespace UserManagement.BackEnd.Application.UserDataPermissions.Services
{
    public record ProjectAccess(string CompanyId, ProjectType ProjectType, int ProjectId, bool IsShared)
    {
        public IList<string> SharedUserIds { get; } = new List<string>();

        public virtual bool Equals(ProjectAccess? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            return CompanyId == other.CompanyId
                   && ProjectType == other.ProjectType
                   && ProjectId == other.ProjectId;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CompanyId, ProjectType, ProjectId);
        }
    }

    public interface IUserDataPermissionsService
    {
        Task<IList<AllVueUserDataPermission>> GetByUserIdAsync(string userId, CancellationToken token);
        Task DeleteAllPermissionsForUserAsync(string userId, CancellationToken token);
        Task<IList<AllVueUserDataPermission>> GetByCompaniesAndProjectAsync(string []companies, ProjectOrProduct project, CancellationToken token);
        Task<AllVueRule> GetByUserIdByCompanyAndProjectAsync(string userId, string company, ProjectOrProduct project, CancellationToken token);
        Task<IList<AllVueRule>> GetAllVueRulesByCompaniesAsync(string[] companies, CancellationToken token);
        Task<IList<AllVueRule>> GetAllVueRulesByProjectAsync(string company, ProjectOrProduct project, CancellationToken token);
        Task<AllVueRule?> GetAllVueRuleById(int id, CancellationToken token);

        Task DeleteAllVueUserDataPermissionAsync(int userDataPermisionsId, CancellationToken token);
        Task<int> AddAllVueRuleAsync(string userId, AllVueRule rule, CancellationToken token);
        Task DeleteAllVueRuleAsync(int ruleId, CancellationToken token);

        Task AssignAllVueRuleToUserDataPermissionAsync(int  userDataPermissionId, int newAllRuleId,
            string updatedByUserId, CancellationToken token);
        Task AssignAllVueRuleToUserDataPermissionAsync(string[] userIds, int allVueRuleId, string updatedByUserId, CancellationToken token);
        Task<int> UpdateAllVueRuleAsync(string updatedByUserId, AllVueRule updatedAllVueRule, CancellationToken token);
        Task SetProjectEnableForAllUserAccessAsync(string project, bool isEnabled, CancellationToken token);
        Task<IList<string>> GetUserIdsAssignedToAllVueRuleAsync(int ruleId, CancellationToken token);
        Task<IList<AllVueRule>> GetSharedProjectsByUserId(string userId, string[] companies, CancellationToken token);
        Task<IList<ProjectAccess>> SummaryProjectAccess(string[] companies, CancellationToken token);
    }

    public class UserDataPermissionsService : IUserDataPermissionsService
    {
        private IUserDataPermissionRepository _dataPermissionRepository;
        private IAllVueRuleRepository _allVueRuleRepository;
        private readonly TimeProvider _timeProvider;

        public UserDataPermissionsService(IUserDataPermissionRepository dataPermissionRepository, IAllVueRuleRepository allVueRuleRepository, TimeProvider timeProvider)
        {
            _allVueRuleRepository = allVueRuleRepository;
            _timeProvider = timeProvider;
            _dataPermissionRepository = dataPermissionRepository;
        }

        public async Task<IList<AllVueUserDataPermission>> GetByUserIdAsync(string userId, CancellationToken token)
        {
            var userDataPermission =  await _dataPermissionRepository.GetByUserIdAsync(userId, token);
            return userDataPermission
                .Where(userDataPermission => userDataPermission is { Rule: EF.AllVueRule })
                .Select(userDataPermission => userDataPermission.ToUserDataPermission())
                .ToList();
        }

        public async Task<IList<AllVueUserDataPermission>> GetByCompaniesAndProjectAsync(string[] companies, ProjectOrProduct project, CancellationToken token)
        {
            return (await _dataPermissionRepository.GetByCompaniesAndAllVueProjectsAsync(companies, project, token)).Select(x=>x.ToUserDataPermission()).ToList();
        }

        public async Task SetProjectEnableForAllUserAccessAsync(string project, bool isEnabled, CancellationToken token)
        {
            //
            //ToDo: Save in  AuthServer
            //

        }
        public async Task<AllVueRule> GetByUserIdByCompanyAndProjectAsync(string userId, string company, ProjectOrProduct project, CancellationToken token)
        {
            //
            //ToDo: Check with AuthServer
            //
            var userDataPermission = await _dataPermissionRepository.GetByUserIdByCompanyAndProjectAsync(userId, company, project, token);
            if (userDataPermission is { Rule: EF.AllVueRule allVueRule})
                return allVueRule.ToAllVueRule();
            var result = await _allVueRuleRepository.GetDefaultByCompanyAndAllVueProjectAsync(company, project, token);
            if (result == null)
                return null;
            return result.ToAllVueRule();
        }

        public async Task<IList<AllVueRule>> GetAllVueRulesByProjectAsync(string company, ProjectOrProduct project,CancellationToken token)
        {
            var rules = await _allVueRuleRepository.GetByCompanyAndProjectId(company,project, token);
            return rules.Select(x => x.ToAllVueRule()).ToList();
        }

        public async Task<IList<AllVueRule>> GetAllVueRulesByCompaniesAsync(string[] companies, CancellationToken token)
        {
            if (companies == null || companies.Length == 0)
                return new List<AllVueRule>();

            var rules = await _allVueRuleRepository.GetByCompaniesAsync(companies, token);
            return rules.Select(x=>x.ToAllVueRule()).ToList();
        }

        public async Task DeleteAllPermissionsForUserAsync(string userId, CancellationToken token)
        {
            await _dataPermissionRepository.DeleteAllPermissionsForUserAsync(userId, token);

        }

        public async Task DeleteAllVueUserDataPermissionAsync(int id, CancellationToken token)
        {
            //
            //ToDo: AuthServer remove user access
            //

            await _dataPermissionRepository.DeleteAsync(id, token);
        }

        public async Task<int> AddAllVueRuleAsync(string userId, AllVueRule rule, CancellationToken token)
        {
            rule.SetUpdated(userId, _timeProvider);
            var dbRule = rule.FromAllVueRule();
            await _allVueRuleRepository.AddAsync(dbRule, token);
            return dbRule.Id;
        }

        public async Task AssignAllVueRuleToUserDataPermissionAsync(int userDataPermissionsId, int newRuleId, string updatedByUserId, CancellationToken token)
        {
            var dbUserDataPermission = await _dataPermissionRepository.GetByIdAsync(userDataPermissionsId, token);
            if (dbUserDataPermission == null)
            {
                throw new ArgumentException($"UserDataPermission with ID {userDataPermissionsId} not found.");
            }
            //
            //ToDo: AuthServer add user access
            //

            var allVueRule = await _allVueRuleRepository.GetById(newRuleId, token);
            if (allVueRule == null)
            {
                throw new ArgumentException($"AllVueRule with ID {newRuleId} not found.");
            }
            dbUserDataPermission.Rule = allVueRule;
            dbUserDataPermission.RuleId = allVueRule.Id;

            dbUserDataPermission.UpdatedByUserId = updatedByUserId;
            dbUserDataPermission.UpdatedDate = _timeProvider.GetUtcNow().UtcDateTime;

            // Save changes
            await _dataPermissionRepository.UpdateAsync(dbUserDataPermission, token);
        }

        public async Task AssignAllVueRuleToUserDataPermissionAsync(string[] userIds, int allVueRuleId, string updatedByUserId, CancellationToken token)
        {
            if (userIds == null)
                throw new ArgumentException("userIds must not be empty.");

            var allVueRule = await _allVueRuleRepository.GetById(allVueRuleId, token);
            if (allVueRule == null)
                throw new ArgumentException($"AllVueRule with ID {allVueRuleId} not found.");

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var project = new ProjectOrProduct(allVueRule.ProjectType, allVueRule.ProjectOrProductId);

            // Fetch all existing permissions for these users
            var existingPermissions = await _dataPermissionRepository.GetByUserIdsAndProjectAsync(userIds, project, token);
            var existingDict = existingPermissions
                .GroupBy(p => p.UserId)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var userId in userIds)
            {
                if (existingDict.TryGetValue(userId, out var existingPermission))
                {
                    // Update existing
                    existingPermission.RuleId = allVueRule.Id;
                    existingPermission.Rule = allVueRule;
                    existingPermission.UpdatedByUserId = updatedByUserId;
                    existingPermission.UpdatedDate = now;
                    await _dataPermissionRepository.UpdateAsync(existingPermission, token);
                }
                else
                {
                    // Create new
                    var newPermission = new UserDataPermission
                    {
                        UserId = userId,
                        RuleId = allVueRule.Id,
                        Rule = allVueRule,
                        UpdatedByUserId = updatedByUserId,
                        UpdatedDate = now
                    };
                    await _dataPermissionRepository.AddAsync(newPermission, token);
                }
            }

            await RemoveUsersNotAssignedToRule(userIds, allVueRuleId, token);
        }

        private async Task RemoveUsersNotAssignedToRule(string[] userIds, int ruleId, CancellationToken token)
        {
            var permissions = await _dataPermissionRepository.GetByRuleId(ruleId, token);
            var permissionIdsToRemove = permissions.Where(p => !userIds.Contains(p.UserId)).Select(p => p.Id).ToList();
            foreach (var permissionId in permissionIdsToRemove)
            {
                await _dataPermissionRepository.DeleteAsync(permissionId, token);
            }
        }

        public async Task DeleteAllVueRuleAsync(int ruleId, CancellationToken token)
        {
            //
            //ToDo: AuthServer add user access
            //

            await _allVueRuleRepository.DeleteAsync(ruleId, token);
        }

        public async Task<int> UpdateAllVueRuleAsync(string updatedByUserId, AllVueRule updatedAllVueRule, CancellationToken token)
        {
            if (updatedAllVueRule == null)
                throw new ArgumentNullException(nameof(updatedAllVueRule));

            var dbRule = await _allVueRuleRepository.GetById(updatedAllVueRule.Id, token);
            if (dbRule == null)
            {
               throw new ArgumentException($"AllVueRule with ID {updatedAllVueRule.Id} not found.");
            }
            if (dbRule.Organisation != updatedAllVueRule.Company)
            {
                throw new ArgumentException($"AllVueRule with ID {updatedAllVueRule.Id} cannot be updated. The company cannot be changed.");
            }
            if (dbRule.ProjectType != updatedAllVueRule.ProjectType || dbRule.ProjectOrProductId != updatedAllVueRule.ProjectId)
            {
                throw new ArgumentException($"AllVueRule with ID {updatedAllVueRule.Id} cannot be updated. The project cannot be changed.");
            }

            dbRule.UpdatedByUserId = updatedByUserId;
            dbRule.UpdatedDate = _timeProvider.GetUtcNow().UtcDateTime;
            dbRule.RuleName = updatedAllVueRule.RuleName;
            dbRule.AllUserAccessForSubProduct = updatedAllVueRule.AllCompanyUsersCanAccessProject;
            dbRule.AvailableVariableIds = updatedAllVueRule.AvailableVariableIds;

            dbRule.Filters = new List<EF.AllVueFilter>();
            foreach (var allVueFilter in updatedAllVueRule.Filters)
            {
                dbRule.Filters.Add(new EF.AllVueFilter(){Id = allVueFilter.Id, AllVueRule = dbRule, AllVueRuleId = dbRule.Id, EntityIds = allVueFilter.EntityIds, EntitySetId = allVueFilter.EntitySetId, VariableConfigurationId = allVueFilter.VariableConfigurationId});
            }
            dbRule.SystemKey = SystemKey.AllVue;

            await _allVueRuleRepository.UpdateAsync(dbRule, token);
            return dbRule.Id;
        }

        public async Task<IList<string>> GetUserIdsAssignedToAllVueRuleAsync(int ruleId, CancellationToken token)
        {
            var permissions = await _dataPermissionRepository.GetByRuleIdAsync(ruleId, token);
            return permissions.Select(permission => permission.UserId).ToList();
        }

        public async Task<AllVueRule?> GetAllVueRuleById(int id, CancellationToken token)
        {
            var dbRule = await _allVueRuleRepository.GetById(id, token);
            return dbRule?.ToAllVueRule();
        }

        private class AllVueRuleProjectComparer : IEqualityComparer<AllVueRule>
        {
            public bool Equals(AllVueRule x, AllVueRule y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x is null || y is null) return false;

                return x.Company == y.Company
                       && x.ProjectType == y.ProjectType
                       && x.ProjectId == y.ProjectId;
            }

            public int GetHashCode(AllVueRule obj)
            {
                if (obj is null) return 0;

                return HashCode.Combine(obj.Company, obj.ProjectType, obj.ProjectId);
            }

        }

        public async Task<IList<AllVueRule>> GetSharedProjectsByUserId(string userId, string[] companies,
            CancellationToken token)
        {
            var userDataAccess = await GetByUserIdAsync(userId, token);
            var sharedWithAllUsersAccess = (await GetAllVueRulesByCompaniesAsync(companies, token)).Where(x => x.AllCompanyUsersCanAccessProject);
            var allItems = userDataAccess.Select(x => x.AllVueRule).Concat(sharedWithAllUsersAccess).ToList();

            return allItems.Distinct(new AllVueRuleProjectComparer()).ToList();
        }

        public async Task<IList<ProjectAccess>> SummaryProjectAccess(string[] companies, CancellationToken token)
        {
            var sharedWithAllUsersAccess = (await GetAllVueRulesByCompaniesAsync(companies, token)).Where(x => x.AllCompanyUsersCanAccessProject);
            var result = sharedWithAllUsersAccess
                .Select(x => new ProjectAccess(x.Company, x.ProjectType, x.ProjectId, true))
                .ToList();
            var items = await _dataPermissionRepository.GetByCompaniesAsync(companies, token);
            foreach (var item in items)
            {
                if (item.Rule is EF.AllVueRule allVueRule)
                {
                    var projectAccess = new ProjectAccess(allVueRule.Organisation, allVueRule.ProjectType, allVueRule.ProjectOrProductId,
                        false);
                    var existing = result.SingleOrDefault(x => x.Equals(projectAccess));
                    if (existing != null)
                    {
                        if (!existing.SharedUserIds.Contains(item.UserId))
                        {
                            existing.SharedUserIds.Add(item.UserId);
                        }
                    }
                    else
                    {
                        projectAccess.SharedUserIds.Add(item.UserId);
                        result.Add(projectAccess);
                    }
                }
            }
            return result;
        }
    }
}