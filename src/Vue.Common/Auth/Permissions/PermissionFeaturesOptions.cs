using System.Text.Json.Serialization;

namespace Vue.Common.Auth.Permissions;

/// <summary>
/// Enum values must match the corresponding entries in the [UserFeaturePermissions].[PermissionOptions] database table.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PermissionFeaturesOptions
{
    // Variables
    VariablesCreate = 2,
    VariablesEdit = 3,
    VariablesDelete = 4,

    // Analysis
    AnalysisAccess = 5,

    // Documents
    DocumentsAccess = 6,

    // Quotas
    QuotasAccess = 7,

    // Settings
    SettingsAccess = 8,

    // Data
    DataAccess = 9,

    // Breaks
    BreaksAdd = 10,
    BreaksEdit = 11,
    BreaksDelete = 13,

    // Reports
    ReportsAddEdit = 14,
    ReportsView = 15,
    ReportsDelete = 16,
}
