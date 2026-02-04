export class Roles {
    public static readonly SystemAdministrator = "SystemAdministrator";
    public static readonly Administrator = "Administrator";
    public static readonly User = "User";
    public static readonly ReportViewer = "ReportViewer";
    public static readonly TrialUser = "TrialUser";
}

export function GetRoleDisplayNameFromName(role: string): string {
    switch (role) {
        case Roles.SystemAdministrator:
            return "Savanta administrator";
        case Roles.Administrator:
            return "Administrator";
        case Roles.User:
            return "Standard user";
        case Roles.ReportViewer:
            return "Report viewer";
        case Roles.TrialUser:
            return "Trial user";
        default:
            return "Unknown role";
    }
}

export function GetRoleDescriptionFromName(role: string): string {
    switch (role) {
        case Roles.SystemAdministrator:
            return "Savanta administrator";
        case Roles.Administrator:
            return "Can configure the project and manage users";
        case Roles.User:
            return "Can view project details, data and reports";
        case Roles.ReportViewer:
            return "Can view reports only";
        case Roles.TrialUser:
            return "Can view the project for a limited period";
        default:
            return "";
    }
}