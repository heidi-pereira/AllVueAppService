using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Authorisation;
using UserManagement.BackEnd.Models;
using EF = BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions.AllVue;

namespace UserManagement.BackEnd.Domain.UserDataPermissions.Entities
{
    public class AllVueRule(int id, string ruleName, bool allCompanyUsersCanAccessProject, string company,
        ProjectType projectType, int projectId, ICollection<int> availableVariableIds, ICollection<AllVueFilter> filters,
        string updatedByUserId, DateTime updatedDate)
    {
        public int Id { get; private set; } = id;
        public string RuleName { get; private set; } = ruleName;

        public bool AllCompanyUsersCanAccessProject { get; private set; } = allCompanyUsersCanAccessProject;

        public string Company { get; private set; } = company;

        public ProjectType ProjectType { get; private set; } = projectType;
        public int ProjectId { get; private set; } = projectId;

        public ICollection<int> AvailableVariableIds { get; private set; } = availableVariableIds;

        public ICollection<AllVueFilter> Filters { get; private set; } = filters;

        public string UpdatedByUserId { get; private set; } = updatedByUserId;
        public DateTime UpdatedDate { get; private set; } = updatedDate;

        protected AllVueRule(): this(0, string.Empty, false, string.Empty, ProjectType.Unknown,0, new List<int>(), new List<AllVueFilter>(), string.Empty,
            DateTime.MinValue)
        {
        }
        public void SetUpdated(string updatedByUserId, TimeProvider dateTimeProvider)
        {
            UpdatedByUserId = updatedByUserId;
            UpdatedDate = dateTimeProvider.GetUtcNow().UtcDateTime;
        }
    }

    public static class AllVueRuleExtensions
    {
        public static EF.AllVueRule? GetSharedToAll(this IList<EF.AllVueRule> existing)
        {
            return existing.SingleOrDefault(x => x.AllUserAccessForSubProduct);
        }
        public static AllVueRule CreateDefaultRole(this IList<AllVueRule> existing, string roleName, string company, ProjectIdentifier projectId, bool shareToAll)
        {
            var baseName = roleName;
            var existingNames = existing.Select(r => r.RuleName).ToHashSet(StringComparer.OrdinalIgnoreCase);

            string uniqueName = baseName;
            int suffix = 1;
            while (existingNames.Contains(uniqueName))
            {
                uniqueName = $"{baseName}~{suffix}";
                suffix++;
            }
            return new AllVueRule(0, uniqueName, shareToAll, company, projectId.Type, projectId.Id, [], [], "", DateTime.MinValue);
        }
        public static AllVueRule ToAllVueRule(this EF.AllVueRule allVueRule)
        {
            return new AllVueRule(allVueRule.Id,
                allVueRule.RuleName,
                allVueRule.AllUserAccessForSubProduct,
                allVueRule.Organisation,
                allVueRule.ProjectType,
                allVueRule.ProjectOrProductId,
                allVueRule.AvailableVariableIds,
                allVueRule.Filters.Select(filter => new AllVueFilter(filter.Id, filter.VariableConfigurationId, filter.EntitySetId, filter.EntityIds)).ToList(),
                allVueRule.UpdatedByUserId,
                allVueRule.UpdatedDate
                );
        }

        public static EF.AllVueRule FromAllVueRule(this AllVueRule allVueRule)
        {
            return new EF.AllVueRule()
            {
                Id = allVueRule.Id,
                RuleName = allVueRule.RuleName,
                AllUserAccessForSubProduct = allVueRule.AllCompanyUsersCanAccessProject,
                Organisation = allVueRule.Company,
                ProjectType = allVueRule.ProjectType,
                ProjectOrProductId = allVueRule.ProjectId,
                AvailableVariableIds = allVueRule.AvailableVariableIds,
                Filters = allVueRule.Filters.Select(filter => new EF.AllVueFilter()
                {
                    Id = filter.Id, 
                    VariableConfigurationId = filter.VariableConfigurationId, 
                    EntitySetId = filter.EntitySetId, 
                    EntityIds = filter.EntityIds,
                    AllVueRuleId = allVueRule.Id
                }).ToList(),
                UpdatedByUserId = allVueRule.UpdatedByUserId,
                UpdatedDate = allVueRule.UpdatedDate,
                SystemKey = SystemKey.AllVue,
            };
        }
    }
}