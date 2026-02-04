import React from "react";
import useGlobalDetailsStore from "../../Model/globalDetailsStore";
import type { IUserContext } from "../../Template/Header/IUserContext";
import { featureGuardHasPermission } from '@shared/FeatureGuard/FeatureGuardPermissionHelper';
import type { IFeatureGuardUser} from '@shared/FeatureGuard/FeatureGuardPermissionHelper';

interface FeatureGuardProps {
    /** Array of enum values */
    permissions?: string[];
    /** When using multiple permissions, whether ANY or ALL must match (default: 'any') */
    match?: 'any' | 'all';
    /** Content to render when user has permission */
    children: React.ReactNode;
    /** Optional fallback content to render when user lacks permission */
    fallback?: React.ReactNode;
    /** Custom permission check function for complex logic */
    customCheck?: (user: IUserContext, isAuthorized: boolean) => boolean;
}

export const FeatureGuard: React.FC<FeatureGuardProps> = ({
    permissions,
    match = 'any',
    children,
    fallback = null,
    customCheck
}) => {
    const user: IUserContext = useGlobalDetailsStore((s) => s.details.user);

    const adaptedCustomCheck = customCheck
        ? (_: IFeatureGuardUser, isAuthorized: boolean) =>
        customCheck(user, isAuthorized)
        : undefined;

    return featureGuardHasPermission(user as IFeatureGuardUser, permissions, match,
            adaptedCustomCheck) ? <>{children}</> : <>{fallback}</>;
};

export default FeatureGuard;
