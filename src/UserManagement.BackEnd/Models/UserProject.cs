using UserManagement.BackEnd.WebApi.Models;

namespace UserManagement.BackEnd.Models
{
    public record UserProject(ProjectIdentifier ProjectIdentifier, string CompanyId, int DataGroupId, string DataGroupName);

    public static class UserProjectExtensions
    {
        public static UserProject ToUserProject(this DataGroup dataGroup)
        {
            return new UserProject(new ProjectIdentifier(dataGroup.ProjectType, dataGroup.ProjectId), dataGroup.Company, dataGroup.Id, dataGroup.RuleName);
        }
    }
}
