using Vue.Common.Auth.Permissions;

namespace OpenEnds.BackEnd.Library
{
    public interface IDataGroupProjectService
    {
        Task<string> GetProjectIdForDataGroupAsync(string surveyId, int questionId);
        Task<DataPermissionDto?> GetDataPermissionsAsync(string surveyId);
    }
}
