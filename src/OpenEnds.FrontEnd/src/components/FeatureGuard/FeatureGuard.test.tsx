import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { FeatureGuard } from './FeatureGuard';

// 1. Mock the module
jest.mock('@shared/FeatureGuard/FeatureGuardPermissionHelper', () => ({
  featureGuardHasPermission: jest.fn(),
}));

// 2. Import the mocked function
import { featureGuardHasPermission } from '@shared/FeatureGuard/FeatureGuardPermissionHelper';

describe('FeatureGuard with mocked featureGuardHasPermission', () => {
  beforeEach(() => {
    // Reset mock before each test
    (featureGuardHasPermission as jest.Mock).mockReset();
  });

  it('renders children when permission is granted by mock', () => {
    // 3. Set up the mock implementation
    (featureGuardHasPermission as jest.Mock).mockReturnValue(true);

    render(
      <FeatureGuard permissions={['VariablesCreate']}>
        <div data-testid="mock-child">Mock Child</div>
      </FeatureGuard>
    );

    expect(screen.getByTestId('mock-child')).toBeInTheDocument();
  });

  it('renders fallback when permission is denied by mock', () => {
    (featureGuardHasPermission as jest.Mock).mockReturnValue(false);

    render(
      <FeatureGuard permissions={['VariablesCreate']} fallback={<div data-testid="mock-fallback">No Access</div>}>
        <div data-testid="mock-child">Mock Child</div>
      </FeatureGuard>
    );

    expect(screen.queryByTestId('mock-child')).not.toBeInTheDocument();
    expect(screen.getByTestId('mock-fallback')).toBeInTheDocument();
  });
});


describe('FeatureGuard customCheck', () => {
    beforeEach(() => {
        (featureGuardHasPermission as jest.Mock).mockReset();
    });

    it('calls customCheck when supplied', () => {
        const customCheck = jest.fn(() => true);

        // Arrange: featureGuardHasPermission should call customCheck
        (featureGuardHasPermission as jest.Mock).mockImplementation(
            (user, _, __, customCheckArg) => {
                if (customCheckArg) {
                    customCheckArg(user, true);
                }
                return true;
            }
        );

        render(
            <FeatureGuard
                permissions={['VariablesCreate']}
                customCheck={customCheck}
            >
                <div>Child</div>
            </FeatureGuard>
        );

        expect(customCheck).toHaveBeenCalledWith(expect.anything(), true);
    });
});