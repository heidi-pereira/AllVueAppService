import React from "react";
import { IUserContext, PermissionFeaturesOptions } from "../../CustomerPortalApi";
import { useProductConfigurationContext } from "../../store/ProductConfigurationContext";
import { featureGuardHasPermission, IFeatureGuardUser } from '../../../../Vue.Common.FrontEnd/Components/FeatureGuard/FeatureGuardPermissionHelper';

interface FeatureGuardProps {
    /** Array of PermissionFeaturesOptions enum values) */
    permissions?: (PermissionFeaturesOptions)[];
    /** When using multiple permissions, whether ANY or ALL must match (default: 'any') */
    match?: 'any' | 'all';
    /** Content to render when user has permission */
    children: React.ReactNode;
    /** Optional fallback content to render when user lacks permission */
    fallback?: React.ReactNode;
    /** Custom permission check function for complex logic */
    customCheck?: (user: IUserContext | null, isAuthorized: boolean) => boolean;
}

export const doesUserHavePermission = (
    user: IUserContext | null,
    permissions?: string[],
    match?: string,
    customCheck?: (user: IUserContext, isAuthorized: boolean) => boolean): boolean => {

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
    customCheck
}) => {
    const { productConfiguration } = useProductConfigurationContext();
    const user = productConfiguration?.user;

    return doesUserHavePermission(user, permissions as string[], match, customCheck) ? <>{children}</> : <>{fallback}</>;
};

export default FeatureGuard;
