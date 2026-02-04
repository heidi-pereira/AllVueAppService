import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { FeatureGuard } from './FeatureGuard';
import { UserContext } from '../../GlobalContext';
import { IApplicationUser, PermissionFeaturesOptions, RunningEnvironment } from '../../BrandVueApi';

jest.mock('FeatureGuardShared/FeatureGuardPermissionHelper', () => ({
    featureGuardHasPermission: jest.fn()
}));
import { featureGuardHasPermission } from 'FeatureGuardShared/FeatureGuardPermissionHelper';

// Mock user factory helpers
const createMockUser = (overrides?: Partial<IApplicationUser>): IApplicationUser => ({
    userId: 'test-user-id',
    userName: 'testuser',
    name: 'Test',
    surname: 'User',
    accountName: 'TestAccount',
    products: ['BrandVue'],
    isAdministrator: false,
    isSystemAdministrator: false,
    isThirdPartyLoginAuth: false,
    isReportViewer: false,
    isTrialUser: false,
    canEditMetricAbouts: false,
    canAccessRespondentLevelDownload: false,
    runningEnvironmentDescription: 'Test Environment',
    runningEnvironment: RunningEnvironment.Development,
    doesUserHaveAccessToInternalSavantaSystems: false,
    featurePermissions: [],
    dataPermission: {
        name: 'Full Access',
        variableIds: [],
        filters: []
    },
    ...overrides
});

const createMockUserWithPermissions = (permissions: Array<{ id: number; code: PermissionFeaturesOptions; name: string; }>): IApplicationUser => 
    createMockUser({
        featurePermissions: permissions
    });

// Test wrapper component for providing UserContext
const TestWrapper: React.FC<{ user: IApplicationUser; children: React.ReactNode }> = ({ user, children }) => (
    <UserContext.Provider value={user}>
        {children}
    </UserContext.Provider>
);

describe('FeatureGuard', () => {
    const testContent = <div data-testid="protected-content">Protected Content</div>;
    const fallbackContent = <div data-testid="fallback-content">Access Denied</div>;

    beforeEach(() => {
        jest.clearAllMocks();
    });

    it('calls permission check with correct parameters', () => {
        const customCheck = jest.fn().mockReturnValue(true);
        (featureGuardHasPermission as jest.Mock).mockReturnValue(true);
        const user = createMockUserWithPermissions([
            { id: 1, code: PermissionFeaturesOptions.VariablesCreate, name: 'Variables Create' }
        ]);
        render(
            <TestWrapper user={user}>
                <FeatureGuard permissions={[PermissionFeaturesOptions.VariablesCreate]} match="all" customCheck={customCheck}>
                    {testContent}
                </FeatureGuard>
            </TestWrapper>
        );
        expect(featureGuardHasPermission).toHaveBeenCalledWith(
            expect.objectContaining({ userId: 'test-user-id' }),
            [PermissionFeaturesOptions.VariablesCreate],
            'all',
            expect.any(Function)
        );
    });

    it('renders children when featureGuardHasPermission returns true', () => {
        (featureGuardHasPermission as jest.Mock).mockReturnValue(true);
        const user = createMockUserWithPermissions([
            { id: 1, code: PermissionFeaturesOptions.VariablesCreate, name: 'Variables Create' }
        ]);
        render(
            <TestWrapper user={user}>
                <FeatureGuard>
                    {testContent}
                </FeatureGuard>
            </TestWrapper>
        );

        expect(screen.getByTestId('protected-content')).toBeTruthy();
    });

    it('renders fallback when featureGuardHasPermission returns false', () => {
        (featureGuardHasPermission as jest.Mock).mockReturnValue(false);
        const user = createMockUserWithPermissions([
            { id: 1, code: PermissionFeaturesOptions.VariablesCreate, name: 'Variables Create' }
        ]);
        render(
            <TestWrapper user={user}>
                <FeatureGuard fallback={fallbackContent}>
                    {testContent}
                </FeatureGuard>
            </TestWrapper>
        );

        expect(screen.getByTestId('fallback-content')).toBeTruthy();
        expect(screen.queryByTestId('protected-content')).toBeNull();
    });

    it('calls customCheck function when provided', () => {
        const customCheck = jest.fn().mockReturnValue(true);
        (featureGuardHasPermission as jest.Mock).mockImplementation((user, permissions, match, customCheck) => {
            if (customCheck) {
                customCheck(user, permissions);
            }
            return true;
        });
        const user = createMockUserWithPermissions([
            { id: 1, code: PermissionFeaturesOptions.VariablesCreate, name: 'Variables Create' }
        ]);
        render(
            <TestWrapper user={user}>
                <FeatureGuard customCheck={customCheck}>
                    {testContent}
                </FeatureGuard>
            </TestWrapper>
        );
        expect(customCheck).toHaveBeenCalled();
    });
});