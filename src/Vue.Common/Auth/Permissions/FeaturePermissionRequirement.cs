using Microsoft.AspNetCore.Authorization;
using Vue.Common.Auth.Permissions;
public class FeaturePermissionRequirement : IAuthorizationRequirement
{
    public PermissionFeaturesOptions[] RequiredFeatureCodes { get; }
    public FeaturePermissionRequirement(params PermissionFeaturesOptions[] requiredFeatureCodes)
    {
        RequiredFeatureCodes = requiredFeatureCodes ?? Array.Empty<PermissionFeaturesOptions>();
    }
}

public static class FeatureRolePolicy
{
    public static string PolicyName(this PermissionFeaturesOptions feature, string role = null)
        => role == null ? feature.ToString() : $"{feature}_{role}";

    public const string VariablesEdit = nameof(PermissionFeaturesOptions.VariablesEdit);
    public const string VariablesCreate = nameof(PermissionFeaturesOptions.VariablesCreate);
    public const string VariablesDelete = nameof(PermissionFeaturesOptions.VariablesDelete);


    public const string VariablesCreate_OR_VariableEdit = nameof(PermissionFeaturesOptions.VariablesCreate) + nameof(PermissionFeaturesOptions.VariablesEdit);
    public const string VariablesCreate_OR_VariableEdit_OR_VariableDelete = nameof(PermissionFeaturesOptions.VariablesCreate) + nameof(PermissionFeaturesOptions.VariablesEdit) + nameof(PermissionFeaturesOptions.VariablesDelete);

    public const string ReportsAddEdit = nameof(PermissionFeaturesOptions.ReportsAddEdit);
    public const string ReportsDelete = nameof(PermissionFeaturesOptions.ReportsDelete);

    public const string Weighting = nameof(PermissionFeaturesOptions.SettingsAccess);
}

