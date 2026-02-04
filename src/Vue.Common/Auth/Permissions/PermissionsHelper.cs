using Vue.Common.Constants.Constants;

namespace Vue.Common.Auth.Permissions
{
    public static class PermissionsHelper
    {
        private static IReadOnlyCollection<PermissionFeatureOptionWithCode> ToPermissionFeatureOptionWithCode(
            params PermissionFeaturesOptions[] options)
        {
            return options.Select(x => new PermissionFeatureOptionWithCode((int)x, x.ToString(), x)).ToList();
        }

        public static IReadOnlyCollection<PermissionFeatureOptionWithCode> DefaultPermissions(this string role)
        {
            switch (role)
            {
                case Roles.Administrator:
                case Roles.SystemAdministrator:
                    return Enum.GetValues(typeof(PermissionFeaturesOptions))
                        .Cast<PermissionFeaturesOptions>()
                        .Select(p => new PermissionFeatureOptionWithCode((int)p, p.ToString(), p))
                        .ToList();

                case Roles.ReportViewer:
                    return ToPermissionFeatureOptionWithCode(PermissionFeaturesOptions.ReportsView);

                case Roles.User:
                default:
                    return ToPermissionFeatureOptionWithCode(PermissionFeaturesOptions.AnalysisAccess,
                        PermissionFeaturesOptions.DocumentsAccess,
                        PermissionFeaturesOptions.QuotasAccess,
                        PermissionFeaturesOptions.DataAccess,
                        PermissionFeaturesOptions.BreaksAdd,
                        PermissionFeaturesOptions.BreaksEdit,
                        PermissionFeaturesOptions.BreaksDelete,
                        PermissionFeaturesOptions.ReportsView
                    );
            }
        }

    }
}