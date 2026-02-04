using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions;
using UserManagement.BackEnd.Domain.UserDataPermissions.Entities;
using EF = BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions.AllVue;

namespace UserManagement.BackEnd.WebApi.Models
{
    public record DataGroup(
        int Id,
        string RuleName,
        bool AllCompanyUsersCanAccessProject,
        string Company,
        ProjectType ProjectType,
        int ProjectId,
        ICollection<int> AvailableVariableIds,
        ICollection<AllVueFilter> Filters,
        ICollection<string> UserIds)
    {
    }

    public static class DataGroupExtensions
    {
        public static DataGroup ToDataGroup(this AllVueRule allVueRule, ICollection<string> userIds)
        {
            return new DataGroup(allVueRule.Id,
                allVueRule.RuleName,
                allVueRule.AllCompanyUsersCanAccessProject,
                allVueRule.Company,
                allVueRule.ProjectType,
                allVueRule.ProjectId,
                allVueRule.AvailableVariableIds,
                allVueRule.Filters,
                userIds);
        }

        public static AllVueRule ToAllVueRule(this DataGroup dataGroup)
        {
            return new AllVueRule(dataGroup.Id,
                dataGroup.RuleName,
                dataGroup.AllCompanyUsersCanAccessProject,
                dataGroup.Company,
                dataGroup.ProjectType,
                dataGroup.ProjectId,
                dataGroup.AvailableVariableIds,
                dataGroup.Filters,
                "",
                DateTime.Now);
        }

        public static DataGroup ToDataGroup(this EF.AllVueRule allVueRule)
        {
            return new DataGroup(
                allVueRule.Id,
                allVueRule.RuleName,
                allVueRule.AllUserAccessForSubProduct,
                allVueRule.Organisation,
                allVueRule.ProjectType,
                allVueRule.ProjectOrProductId,
                allVueRule.AvailableVariableIds,
                allVueRule.Filters.Select(f => new AllVueFilter(f.Id, f.VariableConfigurationId, f.EntitySetId, f.EntityIds)).ToList(),
                []
            );
        }

        public static DataGroup ToDataGroup(this UserDataPermission dataPermission)
        {
            if (dataPermission.Rule is not EF.AllVueRule allVueRule)
            {
                throw new InvalidOperationException($"Expected rule type {nameof(EF.AllVueRule)}, but got {dataPermission.Rule?.GetType().Name ?? "null"}");
            }

            return allVueRule.ToDataGroup() with { UserIds = [dataPermission.UserId] };
        }
    }
}
