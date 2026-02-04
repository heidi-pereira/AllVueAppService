import React from "react";
import { IApplicationUser, PermissionFeaturesOptions } from "../../BrandVueApi";
import { UserContext } from "../../GlobalContext";
import { featureGuardHasPermission, IFeatureGuardUser } from 'FeatureGuardShared/FeatureGuardPermissionHelper';

interface FeatureGuardProps {
    /** Array of PermissionFeaturesOptions enum values) */
    permissions?: (PermissionFeaturesOptions)[];
    /** When using multiple permissions, whether ANY or ALL must match (default: 'any') */
    match?: 'any' | 'all';
    /** Content to render when user has permission (the main component will always be executed and then decided if it will be rendered)*/
    children: React.ReactNode;
    /** Optional fallback content to render when user lacks permission */
    fallback?: React.ReactNode;
    /** Custom permission check function for complex logic specifically around authorization */
    customCheck?: (user: IApplicationUser, isAuthorized: boolean) => boolean;
}


export const doesUserHavePermission = (
    user: IApplicationUser | null,
    permissions?: string[],
    match?: string,
    customCheck?: (user: IApplicationUser, isAuthorized: boolean) => boolean): boolean => {

    const adaptedCustomCheck = customCheck && user
        ? (_: IFeatureGuardUser, isAuthorized: boolean) =>
        customCheck(user, isAuthorized)
        : undefined;

    return (user != null) && featureGuardHasPermission(user as IFeatureGuardUser, permissions, match, adaptedCustomCheck);
}

export const FeatureGuard: React.FC<FeatureGuardProps> = ({
    permissions,
    match = 'any',
    children,
    fallback = null,
    customCheck,
}) => {
    const user: IApplicationUser | null = React.useContext(UserContext);
    return doesUserHavePermission(user, permissions, match, customCheck) ? <>{children}</> : <>{fallback}</>;
};

export default FeatureGuard;
