
export interface IFeatureGuardFeaturePermission {
    code: string;
    name: string; 
};

export interface IFeatureGuardUser {
    featurePermissions?: IFeatureGuardFeaturePermission[]|null;
};

const checkPermission = (user: IFeatureGuardUser, permissions?: string[] , match?: string): boolean => {

    if (!permissions || permissions.length === 0) {
        return true;
    }
    if (!user.featurePermissions || user.featurePermissions.length === 0) {
        return false;
    }

    const permissionChecks = permissions.map((perm: string) => {
        return user.featurePermissions?.some((fp: IFeatureGuardFeaturePermission) => fp.code === perm)??false;
    });

    if (match === 'all') {
        return permissionChecks.every(check => check);
    } else {
        return permissionChecks.some(check => check);
    }
};

export const featureGuardHasPermission = (user: IFeatureGuardUser|null,
    permissions?: string[],
    match?: string,
    customCheck?: (user: IFeatureGuardUser, isAuthorized: boolean) => boolean): boolean => {

    if (!user) {
        return false;
    }

    const allowed = checkPermission(user, permissions, match);

    if (customCheck) {
        return customCheck(user, allowed);
    }
    return allowed;
}