namespace Vue.Common.Auth.Permissions
{
    public record SummaryProjectAccess(
        string CompanyId,
        int ProjectType,
        int ProjectId,
        bool IsShared,
        IList<string> SharedUserIds) 
    {
        public string ToName()
        {
            switch (ProjectType)
            {
                case (int)BrandVue.EntityFramework.MetaData.ProjectType.AllVueSurvey:
                    return $"Survey/{ProjectId}";
                case (int)BrandVue.EntityFramework.MetaData.ProjectType.AllVueSurveyGroup:
                    return $"SurveyGroup/{ProjectId}";
                default:
                    return $"{ProjectType}/{ProjectId}";
            }
        }
    }

    public record DataPermissionFilterDto(int VariableConfigurationId, IList<int> EntityInstanceId);

    public record DataPermissionDto(
        string Name,
        ICollection<int> VariableIds,
        IList<DataPermissionFilterDto> Filters)
    {
        public override string ToString()
        {
            var variableIds = VariableIds != null ? string.Join(",", VariableIds) : "null";
            var filters = Filters != null
                ? string.Join(";", Filters.Select(f =>
                    $"VariableConfigurationId:{f.VariableConfigurationId},EntityInstanceId:[{string.Join(",", f.EntityInstanceId ?? new List<int>())}]"))
                : "null";
            return $"DataPermissionDto(Name: {Name}, VariableIds: [{variableIds}], Filters: [{filters}])";
        }
    }

    public static class DataPermissionDtoExtensions
    {
        public static DataPermissionDto AllPermissions => new DataPermissionDto(string.Empty, Array.Empty<int>(),
            Array.Empty<DataPermissionFilterDto>());
    }
}