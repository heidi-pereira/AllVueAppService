import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import AddFilterButton, { FilterButtonType } from './AddFilterButton';
import { PermissionFeaturesOptions } from '../../../../BrandVueApi';
import { useFilterStateContext } from '../../../../filter/FilterStateContext';
import { mockApplicationUser } from '../../../../helpers/ReactTestingLibraryHelpers';
import { UserContext } from '../../../../GlobalContext';
import { ProductConfigurationContext } from "../../../../ProductConfigurationContext";
import { ProductConfiguration } from '../../../../ProductConfiguration';

const productConfiguration = new ProductConfiguration();
productConfiguration.isSurveyVue = () => true;
productConfiguration.productName = "survey";

jest.mock('../../../../filter/FilterStateContext');
jest.mock('client/state/store', () => {
    const actual = jest.requireActual('client/state/store');
    return {
        ...actual,
        useAppSelector: jest.fn(),
    };
});
jest.mock('../../Variables/VariableModal/VariableContentModal', () => () => <div />);
jest.mock('../../../mixpanel/MixPanel', () => ({
    MixPanel: {
        track: jest.fn(),
        trackPageLoadTime: jest.fn(),
    }
}));

const mockMetrics = [
    { name: 'metric1', varCode: 'M1', isBasedOnCustomVariable: false }
];

const defaultProps = {
    buttonType: FilterButtonType.ShowVariableModal,
    separateVariables: true,
    end: false
};

beforeEach(() => {
    (useFilterStateContext as jest.Mock).mockReturnValue({
        metricsValidAsFilter: mockMetrics,
        filterDispatch: jest.fn()
    });
    jest.clearAllMocks();
});

test('does not show "Create new variable" button if user is null', async () => {
    const mockUser = null;
    render(
        <UserContext.Provider value={mockUser}>
            <ProductConfigurationContext.Provider value={{ productConfiguration: productConfiguration }}>
                <AddFilterButton {...defaultProps} />
            </ProductConfigurationContext.Provider>
        </UserContext.Provider>
    );
    fireEvent.click(screen.getByRole('button', { name: /filter/i }));
    await waitFor(() => {
        expect(screen.queryByText('Create new variable')).not.toBeInTheDocument();
        });
});

test('shows "Create new variable" button if user has VariablesCreate permission', async () => {
    const mockUser = { ...mockApplicationUser,
        isAdministrator: false,
        featurePermissions: [
            { id: 2, name: 'VariablesCreate', code: PermissionFeaturesOptions.VariablesCreate }
        ]
    }
    render(
        <UserContext.Provider value={mockUser}>
            <ProductConfigurationContext.Provider value={{ productConfiguration: productConfiguration }}>
                <AddFilterButton {...defaultProps} />
            </ProductConfigurationContext.Provider>
        </UserContext.Provider>
    );
    fireEvent.click(screen.getByRole('button', { name: /filter/i }));
    await waitFor(() => {
        expect(screen.getByText('Create new variable')).toBeInTheDocument();
        });
});

test('does not show "Create new variable" button if user lacks correct permission and is not admin', async () => {
    const mockUser = { ...mockApplicationUser,
        isAdministrator: false,
        featurePermissions: [
            { id: 3, name: 'VariablesEdit', code: PermissionFeaturesOptions.VariablesEdit }
        ]
    }
    render(
        <UserContext.Provider value={mockUser}>
            <ProductConfigurationContext.Provider value={{ productConfiguration: productConfiguration }}>
            <AddFilterButton {...defaultProps} />
            </ProductConfigurationContext.Provider>
        </UserContext.Provider>
    );
    fireEvent.click(screen.getByRole('button', { name: /filter/i }));
    await waitFor(() => {
        expect(screen.queryByText('Create new variable')).not.toBeInTheDocument();
        });
});