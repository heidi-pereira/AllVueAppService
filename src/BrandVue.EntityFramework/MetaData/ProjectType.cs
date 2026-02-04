using System.ComponentModel;

namespace BrandVue.EntityFramework.MetaData
{
    public enum ProjectType
    {
        Unknown = 0,
        AllVueSurvey = 1,
        AllVueSurveyGroup = 2,
        BrandVue = 3,
    }

    public static class ProjectTypeExtensions
    {
        public static string ToLegacyAuthName(this ProjectType projectType, int projectId,
            IDictionary<int, string> lookupOfIdsToName)
        {
            return projectType switch
            {
                ProjectType.AllVueSurvey => projectId.ToString(),
                ProjectType.AllVueSurveyGroup => lookupOfIdsToName[projectId],
                ProjectType.BrandVue => lookupOfIdsToName[projectId],
                _ => throw new InvalidEnumArgumentException(nameof(projectType), (int)projectType, typeof(ProjectType))
            };
        }

        public static string ToLegacyProductShortCode(this ProjectType projectType)
        {
            return projectType switch
            {
                ProjectType.AllVueSurvey => SavantaConstants.AllVueShortCode,
                ProjectType.AllVueSurveyGroup => SavantaConstants.AllVueShortCode,
                ProjectType.BrandVue => throw new NotImplementedException("BrandVue not implemented"),
                _ => throw new InvalidEnumArgumentException(nameof(projectType), (int)projectType, typeof(ProjectType))
            };
        }
    }

    public record ProjectOrProduct(ProjectType ProjectType, int ProjectId)
    {
        public string ToLegacyAuthName(IDictionary<int, string> lookupOfIdsToName)
        {
            return ProjectType.ToLegacyAuthName(ProjectId, lookupOfIdsToName);
        }
    }
}