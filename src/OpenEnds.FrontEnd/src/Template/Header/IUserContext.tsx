import { PermissionFeaturesOptions } from '../../orval/api/models/permissionFeaturesOptions';

export interface IUserContext {
    products?: string[] | undefined;
    userId?: string | undefined;
    isThirdPartyLoginAuth: boolean;
    isAdministrator: boolean;
    isSystemAdministrator: boolean;
    isReportViewer: boolean;
    isTrialUser: boolean;
    userName?: string | undefined;
    role?: string | undefined;
    currentCompanyShortCode?: string | undefined;
    userCompanyShortCode?: string | undefined;
    firstName?: string | undefined;
    lastName?: string | undefined;
    isInSavantaRequestScope: boolean;
    isAuthorizedSavantaUser: boolean;
    accountName?: string | undefined;
    /** Feature permissions provided by the backend (OpenEnds). If undefined or empty, access defaults to allowed. */
    featurePermissions?: PermissionFeaturesOptions[] | null;
}
