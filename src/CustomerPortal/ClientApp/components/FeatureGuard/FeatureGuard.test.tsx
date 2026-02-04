import React from 'react';
import { render, screen } from '@testing-library/react';
import { FeatureGuard } from './FeatureGuard';
import { useProductConfigurationContext } from '../../store/ProductConfigurationContext';
import { IUserContext, PermissionFeaturesOptions, PermissionFeatureOptionWithCode, ProductConfigurationResult, UserContext } from '../../CustomerPortalApi';

// Mock the context
jest.mock('../../store/ProductConfigurationContext');
const mockUseProductConfigurationContext = useProductConfigurationContext as jest.MockedFunction<typeof useProductConfigurationContext>;

jest.mock('../../../../Vue.Common.FrontEnd/Components/FeatureGuard/FeatureGuardPermissionHelper', () => ({
    featureGuardHasPermission: jest.fn()
}));
import { featureGuardHasPermission } from '../../../../Vue.Common.FrontEnd/Components/FeatureGuard/FeatureGuardPermissionHelper';

// Mock user factory helpers
const createMockUser = (overrides?: Partial<IUserContext>): IUserContext => ({
    userId: 'test-user-id',
    isThirdPartyLoginAuth: false,
    isAdministrator: false,
    isSystemAdministrator: false,
    isReportViewer: false,
    isTrialUser: false,
    canEditMetricAbouts: false,
    userName: 'testuser',
    role: 'user',
    userOrganisation: 'Test Org',
    authCompany: 'Test Company',
    products: ['CustomerPortal'],
    firstName: 'Test',
    lastName: 'User',
    accountName: 'TestAccount',
    isInSavantaRequestScope: false,
    userCompanyShortCode: 'TC',
    isAuthorizedSavantaUser: false,
    featurePermissions: [],
    ...overrides
});

const createMockUserWithPermissions = (permissions: Array<{ id: number; code: PermissionFeaturesOptions; name: string; }>): IUserContext => 
    createMockUser({
        featurePermissions: permissions.map(p => new PermissionFeatureOptionWithCode(p))
    });

const createMockProductConfiguration = (user: IUserContext | null): Partial<ProductConfigurationResult> => ({
    user: user ? new UserContext(user) : undefined,
    googleTags: [],
    vueContext: undefined,
    subdomainOrganisation: '',
    productName: '',
    helpLink: '',
    mixPanelToken: '',
    runningEnvironmentDescription: '',
    runningEnvironment: undefined
});

describe('FeatureGuard', () => {
    const testContent = <div data-testid="protected-content">Protected Content</div>;
    const fallbackContent = <div data-testid="fallback-content">Access Denied</div>;

    beforeEach(() => {
        jest.clearAllMocks();
        const user = createMockUserWithPermissions([
            { id: 1, code: PermissionFeaturesOptions.VariablesCreate, name: 'Variables Create' }
        ]);
        mockUseProductConfigurationContext.mockReturnValue({
            productConfiguration: createMockProductConfiguration(user) as ProductConfigurationResult,
            getDataPageUrl: jest.fn(),
            getReportsPageUrl: jest.fn(),
            getSettingsPageUrl: jest.fn(),
            getOpenEndsPageUrl: jest.fn()
        });
    });

    it('calls permission check with correct parameters', () => {
        const customCheck = jest.fn().mockReturnValue(true);
        (featureGuardHasPermission as jest.Mock).mockReturnValue(true);
        render(
            <FeatureGuard permissions={[PermissionFeaturesOptions.VariablesCreate]} match="all" customCheck={customCheck}>
                {testContent}
            </FeatureGuard>
        );
        expect(featureGuardHasPermission).toHaveBeenCalledWith(
            expect.objectContaining({ userId: 'test-user-id' }),
            [PermissionFeaturesOptions.VariablesCreate],
            'all',
            customCheck
        );
    });

    it('renders children when featureGuardHasPermission returns true', () => {
        (featureGuardHasPermission as jest.Mock).mockReturnValue(true);
        render(
            <FeatureGuard>
                {testContent}
            </FeatureGuard>
        );

        expect(screen.getByTestId('protected-content')).toBeTruthy();
    });

    it('renders fallback when featureGuardHasPermission returns false', () => {
        (featureGuardHasPermission as jest.Mock).mockReturnValue(false);
        render(
            <FeatureGuard fallback={fallbackContent}>
                {testContent}
            </FeatureGuard>
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
        render(
            <FeatureGuard customCheck={customCheck}>
                {testContent}
            </FeatureGuard>
        );
        expect(customCheck).toHaveBeenCalled();
    });
});
